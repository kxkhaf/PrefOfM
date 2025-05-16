using AuthService.API.Configurations.ApiConfig;
using AuthService.API.Middlewares.Configurations;

public static class App
{
    public static void Start(this object obj, string[] args = null!)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApi(builder.Configuration);
        var app = builder.Build();
        app.ConfigureMiddleware();
        app.Run();
    }
}