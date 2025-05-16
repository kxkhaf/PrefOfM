using EmailService.Application.Settings;
using EmailService.Services.Contracts;
using EmailService.Services.Implementations;

namespace EmailService.API.Common;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<EmailTemplateLoader>()
            .AddTransient<IEmailSender, EmailSender>();
    }

    public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .Configure<EmailTemplates>(configuration.GetSection(nameof(EmailTemplates)))
            .Configure<SmtpSettings>(configuration.GetSection(nameof(SmtpSettings)))
            .Configure<AuthServiceSettings>(configuration.GetSection(nameof(AuthServiceSettings)));
    }

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!;
        
        return services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy
                    .WithOrigins(corsSettings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
}