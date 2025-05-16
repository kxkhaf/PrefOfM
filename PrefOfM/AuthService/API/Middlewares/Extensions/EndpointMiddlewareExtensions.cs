using AuthService.API.Middlewares.Configurations;

namespace AuthService.API.Middlewares.Extensions;

public static class EndpointMiddlewareExtensions
{
    public static IApplicationBuilder UseEndpointMapping(this IApplicationBuilder app)
    {
        EndpointMappingMiddleware.UseEndpointMapping(app);
        return app;
    }
}