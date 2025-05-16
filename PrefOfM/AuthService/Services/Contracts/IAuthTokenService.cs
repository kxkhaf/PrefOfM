namespace AuthService.Services.Contracts;

public interface IAuthTokenService
{
    Task SetAuthTokenAsync(HttpContext httpContext);
    Task SetAuthTokenAsync(HttpContext httpContext, string userId);
    Task<(string userId, string deviceId)?> ValidateRefreshTokenAsync(HttpContext httpContext);
}
