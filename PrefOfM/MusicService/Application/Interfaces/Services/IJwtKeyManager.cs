using Microsoft.IdentityModel.Tokens;

namespace MusicService.Application.Interfaces.Services;

public interface IJwtKeyManager
{
    Task<RsaSecurityKey?> GetSigningKeyAsync(CancellationToken ct = default);
    Task<string> GetPublicKeyAsync();
    Task ForceRefreshAsync(CancellationToken ct = default); 
}