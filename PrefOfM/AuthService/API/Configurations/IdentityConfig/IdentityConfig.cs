using AuthService.Application.Settings;
using AuthService.Domain.Models;
using AuthService.Infrastructure.Contexts;
using Microsoft.AspNetCore.Identity;

namespace AuthService.API.Configurations.IdentityConfig;

public static class IdentityConfig
{
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var passwordSettings = configuration.GetSection(nameof(PasswordSettings)).Get<PasswordSettings>()!;

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = passwordSettings.RequiredLength;
                options.Password.RequireDigit = passwordSettings.RequireDigit;
                options.Password.RequireLowercase = passwordSettings.RequireLowercase;
                options.Password.RequireUppercase = passwordSettings.RequireUppercase;
                options.Password.RequireNonAlphanumeric = passwordSettings.RequireNonAlphanumeric;
                options.Password.RequiredUniqueChars = passwordSettings.RequiredUniqueChars;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        

        return services;
    }
}