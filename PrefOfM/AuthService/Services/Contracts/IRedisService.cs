namespace AuthService.Services.Contracts;

public interface IRedisService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    Task DeleteAsync(string key);
    Task<Dictionary<string, string>> GetAllAsync();
    Task DeleteUserKeysAsync(string userId, CancellationToken cancellationToken = default);
}