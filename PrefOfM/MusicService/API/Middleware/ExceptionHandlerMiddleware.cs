using System.Net;
using MusicService.Domain.Exceptions;
using Exception = System.Exception;

namespace MusicService.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return context.Response.WriteAsJsonAsync(new
                {
                    Title = "Validation Error",
                    Status = context.Response.StatusCode,
                    Errors = validationException.Errors
                });
            
            case NotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return context.Response.WriteAsJsonAsync(new
                {
                    Title = "Not Found",
                    Status = context.Response.StatusCode,
                    Detail = exception.Message
                });
            
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return context.Response.WriteAsJsonAsync(new
                {
                    Title = "Server Error",
                    Status = context.Response.StatusCode,
                    Detail = "An unexpected error occurred"
                });
        }
    }
}