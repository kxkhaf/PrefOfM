using AuthService.API.Middlewares.Extensions;
using AuthService.Application.Settings;

namespace AuthService.API.Middlewares.Configurations;

public static class AppMiddlewareConfig
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseCors(app.Configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!.PolicyName)
            .UseCustomSwagger()
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpointMapping();
        return app;
    }
}