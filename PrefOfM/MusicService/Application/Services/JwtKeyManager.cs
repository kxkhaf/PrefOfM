using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MusicService.Application.Features;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Settings;
using MusicService.Constants;

namespace MusicService.Application.Services;

public sealed class JwtKeyManager : IJwtKeyManager, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<JwtSettings> _settingsMonitor;
    private readonly ILogger<JwtKeyManager> _logger;
    private RSA? _rsa;

    public JwtKeyManager(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<JwtSettings> settingsMonitor,
        ILogger<JwtKeyManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsMonitor = settingsMonitor;
        _logger = logger;
        
        _settingsMonitor.OnChange(settings => 
            _logger.LogInformation("JWT settings updated"));
    }

    public async Task<RsaSecurityKey?> GetSigningKeyAsync(CancellationToken ct = default)
    {
        try
        {
            return await LoadKeyFromFileAsync(ct);
        }
        catch
        {
            var jwk = await FetchKeyFromServiceAsync(ct);
            var pem = jwk.ConvertJwkToPem();
            await SaveKeyToFileAsync(pem, ct);
            return CreateSecurityKey(pem);
        }
    }

    public async Task<string> GetPublicKeyAsync()
    {
        using var client = _httpClientFactory.CreateClient(HttpClientConstants.JwtKeyClient);
        using var response = await client.GetAsync(_settingsMonitor.CurrentValue.KeyServiceUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task ForceRefreshAsync(CancellationToken ct = default)
    {
        var key = await LoadKeyWithFallbackAsync(ct);
    }

    private async Task SaveKeyToFileAsync(string pemKey, CancellationToken ct)
    {
        try
        {
            var tempPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempPath, pemKey, ct);

            File.Replace(tempPath, _settingsMonitor.CurrentValue.LocalKeyPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save key to file");
            throw;
        }
    }

    private async Task<RsaSecurityKey> LoadKeyFromFileAsync(CancellationToken ct)
    {
        try
        {
            await using var fileStream = new FileStream(
                _settingsMonitor.CurrentValue.LocalKeyPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = new StreamReader(fileStream);
            var pem = await reader.ReadToEndAsync(ct);
            return CreateSecurityKey(pem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load key from file");
            throw new InvalidOperationException("No valid key available in cache");
        }
    }

    private async Task<RsaSecurityKey> LoadKeyWithFallbackAsync(CancellationToken ct)
    {
        try
        {
            var jwk = await FetchKeyFromServiceAsync(ct);
            var pem = jwk.ConvertJwkToPem();
            await SaveKeyToFileAsync(pem, ct);
            return CreateSecurityKey(pem);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch key from service");
            return await LoadKeyFromFileAsync(ct);
        }
    }

    private async Task<string> FetchKeyFromServiceAsync(CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientConstants.JwtKeyClient);
        using var response = await client.GetAsync(_settingsMonitor.CurrentValue.KeyServiceUrl, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private RsaSecurityKey CreateSecurityKey(string pem)
    {
        _rsa?.Dispose();
        _rsa = RSA.Create();
        _rsa.ImportFromPem(pem);
        return new RsaSecurityKey(_rsa);
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
}