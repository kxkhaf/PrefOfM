// MusicService.Application/Interfaces/Services/ICurrentUserService.cs
namespace MusicService.Infrastructure.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    bool IsAdmin { get; }
}           