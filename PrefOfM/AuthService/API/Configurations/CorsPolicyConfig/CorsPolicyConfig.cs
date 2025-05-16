using AuthService.Application.Settings;

namespace AuthService.API.Configurations.CorsPolicyConfig;

public static class CorsPolicyConfig
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!;
        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(corsSettings.ExposedHeaders);
            });
        });

        return services;
    }
}