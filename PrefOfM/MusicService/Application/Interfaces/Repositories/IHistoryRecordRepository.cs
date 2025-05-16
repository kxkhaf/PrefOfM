using MusicService.Domain.Entities;

namespace MusicService.Application.Interfaces.Repositories;

public interface IHistoryRecordRepository
{
    Task AddAsync(HistoryRecord record);
    Task<IEnumerable<HistoryRecord>> GetUserHistoryAsync(Guid userId, int limit = 50);
    Task ClearUserHistoryAsync(Guid userId);
    Task<IEnumerable<long>> GetLastUniqueSongIdsAsync(Guid userId, int limit);
    Task<IEnumerable<HistoryRecord>> GetRecentRecordsAsync(Guid userId, int limit);
}
