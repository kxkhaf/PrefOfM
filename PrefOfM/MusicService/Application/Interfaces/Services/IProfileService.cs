using MusicService.Application.DTOs;

namespace MusicService.Application.Interfaces.Services;

public interface IProfileService
{
    Task<ProfileDto> GetUserProfileAsync(Guid userId);
}