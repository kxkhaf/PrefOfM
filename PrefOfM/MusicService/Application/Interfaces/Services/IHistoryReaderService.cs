using MusicService.Application.DTOs;

namespace MusicService.Application.Interfaces.Services;

public interface IHistoryReaderService
{
    Task<IEnumerable<HistoryRecordDto>> GetRecentHistoryAsync(Guid userId, int limit);
}