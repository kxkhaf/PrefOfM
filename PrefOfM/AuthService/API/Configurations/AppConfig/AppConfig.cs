using AuthService.API.Configurations.CorsPolicyConfig;
using AuthService.API.Configurations.DatabaseConfig;
using AuthService.API.Configurations.IdentityConfig;
using AuthService.API.Configurations.JwtAuthenticationConfig;
using AuthService.API.Configurations.RedisConfig;
using AuthService.API.Configurations.ServiceDependenciesConfig;
using AuthService.API.Configurations.SettingsConfig;

namespace AuthService.API.Configurations.AppConfig;

public static class AppConfig
{
    public static IServiceCollection AddDependencies(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .ConfigureSettings(configuration)
            .AddDatabase(configuration)
            .AddRedis()
            .AddApplicationServices()
            .AddIdentityConfiguration(configuration)
            .AddJwtAuthentication(configuration)
            .AddCorsPolicy(configuration);
        
        return services;
    }
}