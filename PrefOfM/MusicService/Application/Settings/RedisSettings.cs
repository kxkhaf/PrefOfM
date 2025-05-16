namespace MusicService.Application.Settings;

public class RedisSettings
{
    public string ConnectionString { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;
}