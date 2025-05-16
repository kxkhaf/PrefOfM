namespace MusicService.Application.Settings;

public class JwtSettings
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string KeyServiceUrl { get; init; }
    public required string LocalKeyPath { get; init; }
    public int CacheMinutes { get; init; } = 55;
    public int RefreshHours { get; init; } = 1;
}