namespace AuthService.Application.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenExpiration { get; set; }
    public int RefreshTokenExpiration { get; set; }
    public string RsaPrivateKeyPath { get; set; } = default!;
    public string RsaPublicKeyPath { get; set; } = default!;
    public string KeyId { get; set; } = default!;
    public string KeyType { get; set; } = default!;
    public string PublicKeyUse { get; set; } = default!;
    public string Algorithm { get; set; } = default!;
}