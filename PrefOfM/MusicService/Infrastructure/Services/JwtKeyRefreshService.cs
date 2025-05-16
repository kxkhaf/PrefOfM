using Microsoft.Extensions.Options;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Settings;

namespace MusicService.Infrastructure.Services;

public class JwtKeyRefreshService(
    IJwtKeyManager keyManager,
    IOptionsMonitor<JwtSettings> settingsMonitor,
    ILogger<JwtKeyRefreshService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var refreshInterval = TimeSpan.FromHours(
                    settingsMonitor.CurrentValue.RefreshHours);
                
                await keyManager.ForceRefreshAsync(ct);
                await Task.Delay(refreshInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Key refresh failed");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }
}