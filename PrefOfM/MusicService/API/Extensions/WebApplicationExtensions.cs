using Microsoft.EntityFrameworkCore;
using MusicService.API.Endpoints;
using MusicService.API.Middleware;
using MusicService.Infrastructure.Data;

namespace MusicService.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.MigrateDatabase();
        app.UseRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection()
            .UseCors()
            .UseAuthentication()
            .UseAuthorization()
            .UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }

    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        app.MapSongEndpoints();
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

    private static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}