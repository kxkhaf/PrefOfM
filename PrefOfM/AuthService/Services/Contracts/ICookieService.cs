namespace AuthService.Services.Contracts;
 
public interface ICookieService
{
    void SetRefreshToken(HttpContext context, string refreshToken);
    string? GetRefreshToken(HttpContext context);
    void RemoveRefreshToken(HttpContext context);
}
