using AuthService.Application.Settings;
using AuthService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.API.Configurations.DatabaseConfig;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionStrings = sp.GetRequiredService<IOptions<ConnectionStrings>>().Value;
            if (string.IsNullOrWhiteSpace(connectionStrings.DefaultConnection))
                throw new InvalidOperationException("DefaultConnection string is not configured");

            options.UseNpgsql(connectionStrings.DefaultConnection);
        });

        return services;
    }
}