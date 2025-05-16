using MusicService.Application.DTOs;
using MusicService.Domain.Entities;

namespace MusicService.Application.Interfaces.Services;

public interface IPlaylistService
{
    Task<IEnumerable<PlaylistDto>> GetUserPlaylistsAsync(Guid userId);
    Task<PlaylistDto?> GetPlaylistByIdAsync(long id, Guid userId);
    Task<IEnumerable<PlaylistDto>> GetUserPlaylistsByNameAsync(Guid userId, string name);
    Task<long> CreatePlaylistAsync(CreatePlaylistDto dto, Guid userId);
    Task AddSongToPlaylistAsync(long playlistId, long songId, Guid userId);
    Task AddSongToFavouritesPlaylistAsync(long songId, Guid userId);
    Task RemoveSongFromPlaylistAsync(long playlistId, long songId, Guid userId);
    Task RemoveSongFromFavouritesPlaylistAsync(long songId, Guid userId);
    Task DeletePlaylistAsync(long id, Guid userId);
    Task UpdatePlaylistInfoAsync(UpdatePlaylistDto dto, long playlistId, Guid userId);
    Task<IEnumerable<PlaylistBasicInfoDto>> GetUserPlaylistsBasicInfoAsync(Guid userId);
}
