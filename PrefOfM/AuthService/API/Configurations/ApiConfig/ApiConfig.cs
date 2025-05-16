using AuthService.API.Configurations.ApiServiceConfig;
using AuthService.API.Configurations.AppConfig;

namespace AuthService.API.Configurations.ApiConfig;

public static class ApiConfig
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDependencies(configuration)
            .AddApiServices();
        return services;
    }
}