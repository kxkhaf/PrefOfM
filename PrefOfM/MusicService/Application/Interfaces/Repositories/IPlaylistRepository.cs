using MusicService.Application.DTOs;
using MusicService.Domain.Entities;

namespace MusicService.Application.Interfaces.Repositories;

public interface IPlaylistRepository
{
    Task<Playlist?> GetByIdAsync(long id);
    Task<Playlist?> GetByNameAsync(string name);
    Task<Playlist?> GetByIdWithSongsAsync(long id);
    Task<IEnumerable<Playlist>> GetByUserIdAndNameAsync(Guid userId, string name);
    Task<Playlist> GetPlayilistDataByUserIdAndNameAsync(Guid userId, string name);
    Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Playlist playlist);
    Task UpdateAsync(Playlist playlist);
    Task DeleteAsync(Playlist playlist);
    Task AddSongToPlaylistAsync(long playlistId, long songId);
    Task RemoveSongFromPlaylistAsync(long playlistId, long songId);
    Task<IEnumerable<Playlist>> GetUserPlaylistsBasicInfoAsync(Guid userId);
    Task<List<Guid>> GetUsersWithSimilarTastes(List<long> songIds, Guid excludeUserId);
}