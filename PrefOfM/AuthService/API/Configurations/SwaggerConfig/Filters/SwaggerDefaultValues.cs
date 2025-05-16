using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuthService.API.Configurations.SwaggerConfig.Filters;

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.GroupName != null)
        {
            operation.Tags = new List<OpenApiTag> 
            { 
                new OpenApiTag { Name = context.ApiDescription.GroupName } 
            };
        }
    }
}