using System.Security.Cryptography;
using AuthService.Application.Settings;
using AuthService.Services.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services.Implementations;

public class RsaKeyService : IRsaKeyService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _keyId;

    public RsaKeyService(IOptions<JwtSettings> jwtSettings)
    {
        if (jwtSettings?.Value == null)
            throw new ArgumentNullException(nameof(jwtSettings));

        _keyId = jwtSettings.Value.KeyId;
        
        _privateKey = LoadRsaKey(jwtSettings.Value.RsaPrivateKeyPath);
        _publicKey = LoadRsaKey(jwtSettings.Value.RsaPublicKeyPath);
    }

    public RsaSecurityKey GetPrivateKey() => _privateKey;
    public RsaSecurityKey GetPublicKey() => _publicKey;
    public string KeyId => _keyId;

    private static RsaSecurityKey LoadRsaKey(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
            throw new ArgumentException("Key path cannot be null or empty", nameof(keyPath));

        if (!File.Exists(keyPath))
            throw new FileNotFoundException("RSA key file not found", keyPath);

        try
        {
            var rsa = RSA.Create();
            var keyText = File.ReadAllText(keyPath);
            rsa.ImportFromPem(keyText);
            return new RsaSecurityKey(rsa);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load RSA key", ex);
        }
    }
}