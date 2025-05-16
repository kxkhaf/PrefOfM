namespace EmailService.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenApi()
            .AddHttpClient()
            .AddConfiguration(builder.Configuration)
            .AddApplicationServices()
            .AddCustomCors(builder.Configuration);

        return builder;
    }
}