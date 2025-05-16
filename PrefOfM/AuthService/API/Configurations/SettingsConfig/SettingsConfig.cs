using AuthService.Application.Settings;

namespace AuthService.API.Configurations.SettingsConfig;

public static class SettingsConfig
{
    public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        services.Configure<CorsSettings>(configuration.GetSection(nameof(CorsSettings)));
        services.Configure<PasswordSettings>(configuration.GetSection(nameof(PasswordSettings)));
        services.Configure<EncryptionSettings>(configuration.GetSection(nameof(EncryptionSettings)));
        services.Configure<ConnectionStrings>(configuration.GetSection(nameof(ConnectionStrings)));
        services.Configure<MailServiceSettings>(configuration.GetSection(nameof(MailServiceSettings)));

        services.AddOptions<EncryptionSettings>()
            .Configure(settings =>
            {
                if (Convert.FromBase64String(settings.Key).Length is 0 ||
                    Convert.FromBase64String(settings.HmacKey).Length is 0)
                {
                    throw new ApplicationException("Invalid encryption key length");
                }
            });

        return services;
    }
}