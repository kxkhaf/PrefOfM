// Infrastructure/Cache/IRedisCacheService.cs

using System.Text.Json;
using StackExchange.Redis;

namespace MusicService.Application.Services;

public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    Task DeleteAsync(string key);
    Task DeleteByPatternAsync(string pattern);
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);
    Task SetHashAsync<T>(string hashKey, string key, T value);
    Task<T?> GetHashAsync<T>(string hashKey, string key);
    Task DeleteHashAsync(string hashKey, string key);
}


public class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : IRedisCacheService
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();
    private readonly IServer _server = connectionMultiplexer.GetServers().First();

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(json!);
    }

    public Task<bool> ExistsAsync(string key) => _db.KeyExistsAsync(key);

    public Task DeleteAsync(string key) => _db.KeyDeleteAsync(key);

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        var keys = new List<string>();
            
        await foreach (var redisKey in _server.KeysAsync(pattern: pattern + "*"))
        {
            keys.Add(redisKey.ToString());
        }

        return keys.Distinct();
    }

    public async Task DeleteByPatternAsync(string pattern)
    {
        var keys = new List<RedisKey>();
            
        await foreach (var redisKey in _server.KeysAsync(pattern: pattern + "*"))
        {
            keys.Add(redisKey);
        }

        if (keys.Count > 0)
        {
            await _db.KeyDeleteAsync(keys.ToArray());
        }
    }

    public async Task SetHashAsync<T>(string hashKey, string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.HashSetAsync(hashKey, key, json);
    }

    public async Task<T?> GetHashAsync<T>(string hashKey, string key)
    {
        var json = await _db.HashGetAsync(hashKey, key);
        return json.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(json!);
    }

    public Task DeleteHashAsync(string hashKey, string key)
        => _db.HashDeleteAsync(hashKey, key);
}