using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;

namespace MusicService.Application.Services;

public class HistoryReaderService(IHistoryRecordRepository historyRepository) : IHistoryReaderService
{
    public async Task<IEnumerable<HistoryRecordDto>> GetRecentHistoryAsync(Guid userId, int limit)
    {
        var records = await historyRepository.GetRecentRecordsAsync(userId, limit);
        return records.Select(r => new HistoryRecordDto(
            r.SongId,
            r.Song.Title,
            r.Song.Artist,
            r.PlayedAt,
            r.Context
        ));
    }
}