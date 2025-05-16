using AuthService.Application.Settings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.API.Configurations.RedisConfig;

public static class RedisConfig
{
    public static IServiceCollection AddRedis(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisSettings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return ConnectionMultiplexer.Connect(redisSettings.ConnectionString);
        });

        return services;
    }
}