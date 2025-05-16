using AuthService.API.Endpoints.Account.Configuration.AuthEndpoints;
using AuthService.API.Endpoints.Account.Configuration.ProfileEndpoints;
using AuthService.API.Endpoints.Account.Configuration.WellKnownEndpoints;
using AuthService.API.Endpoints.Contracts;
using AuthService.API.Endpoints.Registry;

namespace AuthService.API.Endpoints.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddApplicationEndpoints(this IServiceCollection services)
    {
        services.AddScoped<IEndpointGroup, AuthEndpoints>();
        services.AddScoped<IEndpointGroup, ProfileEndpoints>();
        services.AddScoped<IEndpointGroup, WellKnownEndpoints>();
        
        services.AddSingleton<EndpointRegistry>();
        
        return services;
    }
}