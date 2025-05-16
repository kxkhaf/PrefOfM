using MusicService.Application.DTOs;
using MusicService.Application.DTOs.Requests;
using MusicService.Domain.Entities;

namespace MusicService.Application.Interfaces.Services;

public interface ISongService
{
    Task<IEnumerable<SongDto>> GetAllSongsAsync();
    Task<IEnumerable<SongDto>> GetSongsAsync(int pageNum, int limit = 50);
    Task<SongDto?> GetSongByIdAsync(long id);
    Task<IEnumerable<SongDto>> GetSongsByEmotionAsync(string emotion, int limit = 50);
    Task<long> CreateSongAsync(CreateSongDto dto);
    Task<List<SongDto>> SearchSongsAsync(string query, int pageNum, int limit = 50);
    Task<IEnumerable<SongDto>> GetSongsByIdsAsync(IEnumerable<long> ids);
    Task<IEnumerable<SongDto>> GetRecentlyPlayedSongsAsync(Guid userId, int limit);
    Task<PaginatedResponse<SongDto>> GetPaginatedSongsAsync(int pageNum, int limit);
    Task<int> GetTotalSongCountAsync();
}