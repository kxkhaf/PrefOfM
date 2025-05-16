using EmailService.API.Endpoints;
using EmailService.Application.Settings;

namespace EmailService.API.Extensions;

public static class WebApplicationExtensions
{
    
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseCorsFromConfiguration();
        app.UseHttpsRedirection();
        app.UseRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapEmailEndpoints();

        return app;
    }

    private static WebApplication UseCorsFromConfiguration(this WebApplication app)
    {
        var corsSettings = app.Configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!;
        app.UseCors(corsSettings.PolicyName);
        return app;
    }

    private static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("→ {Method} {Path}", context.Request.Method, context.Request.Path);

            await next();

            logger.LogInformation("← {Method} {Path} → {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        });
        return app;
    }
}