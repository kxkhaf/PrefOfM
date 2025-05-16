using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using AuthService.Services.Contracts;
using Microsoft.AspNetCore.Authentication;

namespace AuthService.Services.Implementations;

public class AuthTokenService(
    IJwtService jwtService,
    ICookieService cookieService,
    IAuthenticationSchemeProvider schemeProvider,
    ILogger<AuthTokenService> logger) : IAuthTokenService
{
    public async Task SetAuthTokenAsync(HttpContext httpContext)
    {
        try
        {
            var refreshToken = cookieService.GetRefreshToken(httpContext);
            if (refreshToken is not null)
            {
                var tokens = await jwtService.RefreshTokensAsync(refreshToken);
                var sub = new JwtSecurityTokenHandler().ReadJwtToken(tokens.newAccessToken.Value).Claims
                    .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                await SetAuthTokenAsync(httpContext, sub!);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set authentication response.");
            throw;
        }
    }

    public async Task SetAuthTokenAsync(HttpContext httpContext, string userId)
    {
        try
        {
            var (accessToken, refreshToken) = await jwtService.GenerateTokensAsync(userId);

            cookieService.SetRefreshToken(httpContext, refreshToken);

            var scheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync()
                         ?? throw new InvalidOperationException("No default authentication scheme configured");

            httpContext.Response.Headers.Authorization =
                new AuthenticationHeaderValue(scheme.Name, accessToken.Value).ToString();

            logger.LogInformation("Authentication tokens set for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set authentication response for user {UserId}", userId);
            throw;
        }
    }
    
    public async Task<(string userId, string deviceId)?> ValidateRefreshTokenAsync(HttpContext httpContext)
    {
        var refreshToken = cookieService.GetRefreshToken(httpContext);
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("Refresh token is missing in request");
            return null;
        }

        return await jwtService.ValidateAndDecryptRefreshTokenAsync(refreshToken);
    }

}