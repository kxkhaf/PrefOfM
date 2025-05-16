using MusicService.Application.Interfaces.Services;
using System.Security.Claims;

namespace MusicService.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public string? Username => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(ClaimTypes.Role) ?? false;
}