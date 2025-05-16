namespace MusicService.Application.Interfaces.Services;

public interface IJwtRequestReader
{
    Task<(bool Success, Guid? UserId, IReadOnlyDictionary<string, string>? Claims, string? Error)> 
        GetValidatedUserClaimsAsync(HttpContext context);
}