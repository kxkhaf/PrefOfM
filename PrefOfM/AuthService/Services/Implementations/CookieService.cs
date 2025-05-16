using System.Text;
using AuthService.Application.Settings;
using AuthService.Domain.Constants;
using AuthService.Services.Contracts;
using Microsoft.Extensions.Options;

namespace AuthService.Services.Implementations;

public class CookieService(IOptions<JwtSettings> jwtSettings) : ICookieService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public void SetRefreshToken(HttpContext context, string refreshToken)
    {
        var encodedRefreshToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(refreshToken));
        context.Response.Cookies.Append(
            CookieKeys.RefreshToken,
            encodedRefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiration)
            });
    }

    public string? GetRefreshToken(HttpContext context)
    {
        var encodedRefreshToken = context.Request.Cookies[CookieKeys.RefreshToken];
        return encodedRefreshToken != null ? Encoding.UTF8.GetString(Convert.FromBase64String(encodedRefreshToken)) : null;
    }

    public void RemoveRefreshToken(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieKeys.RefreshToken);
    }
}