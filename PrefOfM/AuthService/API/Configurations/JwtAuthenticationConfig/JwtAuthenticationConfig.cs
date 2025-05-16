using AuthService.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthService.API.Configurations.JwtAuthenticationConfig;

public static class JwtAuthenticationConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => JwtBearerConfigurator.Configure(options, configuration));

        return services;
    }
}