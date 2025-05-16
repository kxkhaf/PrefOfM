using AuthService.API.Configurations.SwaggerConfig;
using AuthService.API.Endpoints.Extensions;

namespace AuthService.API.Configurations.ApiServiceConfig;

public static class ApiServiceConfig
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddCustomSwaggerGen();
        services.AddAuthorization();
        services.AddApplicationEndpoints();

        return services;
    }
}