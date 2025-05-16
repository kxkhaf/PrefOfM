using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services.Contracts;

public interface IRsaKeyService
{
    RsaSecurityKey GetPrivateKey();
    RsaSecurityKey GetPublicKey();
    string KeyId { get; }
}