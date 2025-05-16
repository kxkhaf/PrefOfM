using AuthService.API.Endpoints.Contracts;

namespace AuthService.API.Endpoints.Registry;

public class EndpointRegistry(IServiceProvider serviceProvider)
{
    public void MapEndpoints(WebApplication app)
    {
        using var scope = serviceProvider.CreateScope();
        var endpointGroups = scope.ServiceProvider.GetServices<IEndpointGroup>();
        
        foreach (var group in endpointGroups)
        {
            group.MapEndpoints(app);
        }
    }
}