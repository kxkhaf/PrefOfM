using AuthService.Infrastructure.Configurations;
using AuthService.Services.Contracts;
using AuthService.Services.Implementations;
using AuthService.Utils;

namespace AuthService.API.Configurations.ServiceDependenciesConfig;

public static class ServiceDependenciesConfig
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IMailServiceClient, MailServiceClient>();
        services.AddScoped<ITokenEncryptor, TokenEncryptor>();
        services.AddScoped<HttpClient>();
        
        services.AddSingleton<IRsaKeyService, RsaKeyService>();
        services.AddSingleton<JwtBearerConfigurator>();
        return services;
    }
}