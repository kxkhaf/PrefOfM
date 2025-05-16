namespace AuthService.Infrastructure.Redis;

public static class RedisKeyBuilder
{
    public static string RefreshToken(string userId, string deviceId)
        => $"refresh:{userId}:{deviceId}";

    public static string RefreshTokenPattern(string userId = "*", string deviceId = "*")
        => $"refresh:{userId}:{deviceId}";
    
    public static string Session(string userId, string deviceId)
        => $"session:{userId}:{deviceId}";

    public static string SessionPattern(string userId = "*", string deviceId = "*")
        => $"session:{userId}:{deviceId}";
}