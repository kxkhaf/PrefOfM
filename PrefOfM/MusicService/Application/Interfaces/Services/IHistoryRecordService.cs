using MusicService.Application.DTOs;
using MusicService.Application.Services;

namespace MusicService.Application.Interfaces.Services;

public interface IHistoryRecordService
{
    Task AddRecordAsync(Guid userId, long songId, string? context = null);
    Task<IEnumerable<HistoryRecordDto>> GetUserHistoryAsync(Guid userId, int limit = 50);
    Task ClearUserHistoryAsync(Guid userId);
    Task<IEnumerable<SongDto>> GetLastUniqueSongsAsync(Guid userId, int limit);
}
