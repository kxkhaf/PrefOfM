using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthService.Application.Settings;
using AuthService.Domain.Constants;
using AuthService.Domain.Models;
using AuthService.Services.Contracts;
using AuthService.Utils;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services.Implementations;

public class JwtService(
    IOptions<JwtSettings> jwtSettings,
    IRsaKeyService rsaKeyService,
    IRedisService redisService,
    ITokenEncryptor tokenEncryptor)
    : IJwtService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private const char TokenSeparator = '|';

    public async Task<(Token accessToken, string encryptedRefreshToken)> GenerateTokensAsync(string userId)
    {
        var deviceId = DeviceIdGenerator.GenerateDeviceId();
        var accessToken = GenerateAccessToken(userId, deviceId);
        var refreshToken = await GenerateEncryptedRefreshTokenAsync(userId, deviceId);
        return (accessToken, refreshToken);
    }

    public Token GenerateAccessToken(string userId, string deviceId)
    {
        var signingCredentials = new SigningCredentials(
            rsaKeyService.GetPrivateKey(),
            SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(AuthClaims.DeviceId, deviceId)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: signingCredentials);

        return new Token
        (
            Value: new JwtSecurityTokenHandler().WriteToken(token),
            Expiration: expires
        );
    }

    public async Task<string> GenerateEncryptedRefreshTokenAsync(string userId, string deviceId)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenData = $"{refreshToken}{TokenSeparator}{userId}{TokenSeparator}{deviceId}";
        var encryptedToken = tokenEncryptor.Encrypt(tokenData);
        
        await redisService.SetAsync(
            GetRefreshKey(userId, deviceId),
            encryptedToken,
            TimeSpan.FromDays(_jwtSettings.RefreshTokenExpiration));
        
        return encryptedToken;
    }

    public ClaimsPrincipal ValidateAccessToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = rsaKeyService.GetPublicKey(),
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };

        return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
    }
    
    public async Task<bool> ValidateRefreshTokenAsync(string encryptedRefreshToken)
    {
        try
        {
            var (_, _) = await ValidateAndDecryptRefreshTokenAsync(encryptedRefreshToken);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<(string userId, string deviceId)> ValidateAndDecryptRefreshTokenAsync(string encryptedRefreshToken)
    {
        var decryptedToken = tokenEncryptor.Decrypt(Uri.UnescapeDataString(encryptedRefreshToken));
        var parts = decryptedToken.Split(TokenSeparator);
        
        if (parts.Length != 3)
            throw new SecurityTokenException("Invalid token format");

        var storedToken = await redisService.GetAsync<string>(GetRefreshKey(parts[1], parts[2]));

        if (storedToken != encryptedRefreshToken)
            throw new SecurityTokenException("Refresh token is invalid or expired");

        return (parts[1], parts[2]);
    }

    public async Task<(Token newAccessToken, string newEncryptedRefreshToken)> RefreshTokensAsync(string encryptedRefreshToken)
    {
        var (userId, deviceId) = await ValidateAndDecryptRefreshTokenAsync(encryptedRefreshToken);
        await redisService.DeleteAsync(GetRefreshKey(userId, deviceId));
        return await GenerateTokensAsync(userId);
    }

    public async Task LogoutDeviceAsync(string encryptedRefreshToken)
    {
        var (userId, deviceId) = await ValidateAndDecryptRefreshTokenAsync(encryptedRefreshToken);
        await redisService.DeleteAsync(GetRefreshKey(userId, deviceId));
    }

    public async Task LogoutAllDevicesAsync(string encryptedRefreshToken)
    {
        var (userId, _) = await ValidateAndDecryptRefreshTokenAsync(encryptedRefreshToken);
        await redisService.DeleteUserKeysAsync(userId);
    }

    public (string userId, string deviceId) GetTokenClaims(string accessToken)
    {
        var principal = ValidateAccessToken(accessToken);
        return (
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new SecurityTokenException("Missing user ID"),
            principal.FindFirstValue(AuthClaims.DeviceId) ?? throw new SecurityTokenException("Missing device ID")
        );
    }

    private static string GetRefreshKey(string userId, string deviceId) => $"refresh:{userId}:{deviceId}";
}