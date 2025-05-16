using EmailService.Application.Settings;
using EmailService.Services.Contracts;
using EmailService.Services.Implementations;

namespace EmailService.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailTemplates>(configuration.GetSection(nameof(EmailTemplates)));
        services.Configure<SmtpSettings>(configuration.GetSection(nameof(SmtpSettings)));
        services.Configure<AuthServiceSettings>(configuration.GetSection(nameof(AuthServiceSettings)));
        
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<EmailTemplateLoader>();
        services.AddTransient<IEmailSender, EmailSender>();
        
        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!;
        
        services.AddCors(options => options.AddPolicy(
            corsSettings.PolicyName,
            policy => policy
                .WithOrigins(corsSettings.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));
        
        return services;
    }
}