using System.Text.Json;
using AuthService.Infrastructure.Redis;
using AuthService.Services.Contracts;
using StackExchange.Redis;

namespace AuthService.Services.Implementations;

public class RedisService(IConnectionMultiplexer connection, ILogger<RedisService> logger) : IRedisService
{
    private readonly IDatabase _db = connection.GetDatabase();

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

    public Task<bool> ExistsAsync(string key)
        => _db.KeyExistsAsync(key);

    public Task DeleteAsync(string key)
        => _db.KeyDeleteAsync(key);
    
    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        var endpoints = _db.Multiplexer.GetEndPoints();
        var server = _db.Multiplexer.GetServer(endpoints[0]);

        var keys = server.Keys();

        var result = new Dictionary<string, string>();

        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            var strKey = key.ToString();
            result[strKey] = value!;
            logger.LogInformation("{Key}  -:-  {Value}", strKey, value);
        }

        return result;
    }
    
    public async Task DeleteUserKeysAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = RedisKeyBuilder.RefreshTokenPattern(userId);
        var endpoints = _db.Multiplexer.GetEndPoints();
        var server = _db.Multiplexer.GetServer(endpoints[0]);

        var keysToDelete = new List<RedisKey>();
        var cursor = 0L;

        do
        {
            var result = await _db.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", pattern);
            {
                var values = (RedisResult[])result!;

                cursor = (long)values[0];
                var keys = (RedisKey[])values[1]!;

                keysToDelete.AddRange(keys);
            }

            cancellationToken.ThrowIfCancellationRequested();

        } while (cursor != 0);
        
        if (keysToDelete.Any())
        {
            await _db.KeyDeleteAsync(keysToDelete.ToArray());
        }
    }


}