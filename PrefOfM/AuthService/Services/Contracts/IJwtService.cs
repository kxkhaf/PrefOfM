using System.Security.Claims;
using AuthService.Domain.Models;

namespace AuthService.Services.Contracts;

public interface IJwtService
{
    Task<(Token accessToken, string encryptedRefreshToken)> GenerateTokensAsync(string userId);
    Token GenerateAccessToken(string userId, string deviceId);
    Task<string> GenerateEncryptedRefreshTokenAsync(string userId, string deviceId);
    ClaimsPrincipal ValidateAccessToken(string token);
    Task<(string userId, string deviceId)> ValidateAndDecryptRefreshTokenAsync(string encryptedRefreshToken);
    Task<(Token newAccessToken, string newEncryptedRefreshToken)> RefreshTokensAsync(string encryptedRefreshToken);
    Task LogoutDeviceAsync(string encryptedRefreshToken);
    Task LogoutAllDevicesAsync(string encryptedRefreshToken);
    (string userId, string deviceId) GetTokenClaims(string accessToken);
    Task<bool> ValidateRefreshTokenAsync(string encryptedRefreshToken);
}