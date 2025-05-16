

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MusicService.Application.Settings;

namespace MusicService.Infrastructure.Identity;

public class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    private readonly JwtSettings _jwtSettings = options.Value;

    
    /// <summary>
    /// Валидирует токен и возвращает ClaimsPrincipal.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(_jwtSettings.LocalKeyPath));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validationParameters, out _);
            
            return principal;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Читает данные из JWT БЕЗ проверки подписи (только для доверенных случаев!).
    /// </summary>
    public ClaimsPrincipal ReadTokenWithoutValidation(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var claimsIdentity = new ClaimsIdentity(jwtToken.Claims);
        return new ClaimsPrincipal(claimsIdentity);
    }
}