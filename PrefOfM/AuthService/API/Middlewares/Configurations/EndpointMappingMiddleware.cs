using AuthService.API.Endpoints.Registry;

namespace AuthService.API.Middlewares.Configurations;

public class EndpointMappingMiddleware(
    RequestDelegate next,
    EndpointRegistry endpointRegistry)
{
    private readonly EndpointRegistry _endpointRegistry = endpointRegistry;

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
    }

    public static void UseEndpointMapping(IApplicationBuilder app)
    {
        var endpointRegistry = app.ApplicationServices.GetRequiredService<EndpointRegistry>();
        endpointRegistry.MapEndpoints((WebApplication)app);
    }
}