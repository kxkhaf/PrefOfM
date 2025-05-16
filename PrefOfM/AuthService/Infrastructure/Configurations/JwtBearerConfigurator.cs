using System.Security.Cryptography;
using AuthService.Application.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Configurations;

public class JwtBearerConfigurator
{
    private readonly RsaSecurityKey _publicKey;

    public JwtBearerConfigurator(IOptions<JwtSettings> jwtSettings)
    {
        var jwtSettings1 = jwtSettings.Value;
        _publicKey = LoadPublicKey(jwtSettings1.RsaPublicKeyPath);
    }

    public static void Configure(JwtBearerOptions options, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()!;
        var publicKey = LoadPublicKey(jwtSettings.RsaPublicKeyPath);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = publicKey,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.Zero
        };
    }

    private static RsaSecurityKey LoadPublicKey(string keyPath)
    {
        if (!File.Exists(keyPath))
            throw new FileNotFoundException("Public key file not found", keyPath);

        var rsa = RSA.Create();
        var keyText = File.ReadAllText(keyPath);
        rsa.ImportFromPem(keyText);
        return new RsaSecurityKey(rsa);
    }
}