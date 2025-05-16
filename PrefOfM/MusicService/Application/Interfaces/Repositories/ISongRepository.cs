using MusicService.Domain.Entities;
using MusicService.Domain.Enums;

namespace MusicService.Application.Interfaces.Repositories;

public interface ISongRepository
{
    Task DeleteAsync(Song song);
    Task UpdateAsync(Song song);
    Task AddAsync(Song song);
    Task<IEnumerable<Song>> GetByEmotionAsync(Emotion emotion);
    Task<IEnumerable<Song>> GetByEmotionAsync(Emotion emotion, int limit);
    Task<IEnumerable<Song>> GetAllAsync();
    Task<IEnumerable<Song>> GetSongsAsync(int pageNum,int limit);
    Task<Song?> GetByIdAsync(long id);
    Task<List<Song>> SearchSongsAsync(string query, int pageNum, int limit = 50);
    Task<IEnumerable<Song>> GetByIdsAsync(IEnumerable<long> ids);
    Task<List<Song>> GetRecommendedByUsersAsync(List<Guid> similarUserIds, List<long> excludeSongIds, string? query = null);
    Task<List<Song>> GetPopularSongsAsync(string? query = null);
    Task<List<Song>> GetRecentlyListenedRecommendationsAsync(Guid userId, string? query = null);
    Task<int> GetTotalSongCountAsync();
    Task<List<Song>> GetRandomSongsAsync(int limit, string? query = null);
    Task<List<Song>> GetRandomSongsByEmotionAsync(int count, Emotion emotion, string? query = null);
}