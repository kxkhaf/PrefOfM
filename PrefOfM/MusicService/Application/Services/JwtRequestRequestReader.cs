using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Settings;
using MusicService.Infrastructure.Identity;

namespace MusicService.Application.Services;

public class JwtRequestReader(IJwtService jwtService, IOptions<AuthSettings> authSettings)
    : IJwtRequestReader
{
    public async Task<(bool Success, Guid? UserId, IReadOnlyDictionary<string, string>? Claims, string? Error)> 
        GetValidatedUserClaimsAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(authSettings.Value.AuthHeader, out var authHeader))
            return (false, null, null, "Authorization header is missing");
        
        var token = authHeader.ToString().Replace(
            authSettings.Value.Prefix, 
            "", 
            StringComparison.OrdinalIgnoreCase);
        
        ClaimsPrincipal principal;
        try
        {
            principal = jwtService.ReadTokenWithoutValidation(token);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Invalid token: {ex.Message}");
        }
        
        var claimsDict = principal.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(
                g => g.Key, 
                g => g.First().Value,
                StringComparer.OrdinalIgnoreCase);

        if (!claimsDict.TryGetValue(JwtRegisteredClaimNames.Sub, out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            return (false, null, null, "Invalid or missing user ID (sub claim)");
        }

        return (true, userId, claimsDict, null);
    }
}