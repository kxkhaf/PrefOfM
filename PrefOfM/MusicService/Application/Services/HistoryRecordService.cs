using AutoMapper;
using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;
using MusicService.Domain.Entities;

namespace MusicService.Application.Services;

public class HistoryRecordService(
    IHistoryRecordRepository historyRepository,
    ISongService songService,
    IMapper mapper) : IHistoryRecordService
{
    public async Task AddRecordAsync(Guid userId, long songId, string? context = null)
    {
        var record = new HistoryRecord
        {
            UserId = userId,
            SongId = songId,
            PlayedAt = DateTime.UtcNow,
            Context = context
        };
        
        await historyRepository.AddAsync(record);
    }

    public async Task<IEnumerable<HistoryRecordDto>> GetUserHistoryAsync(Guid userId, int limit = 50)
    {
        var records = await historyRepository.GetUserHistoryAsync(userId, limit);
        return records.Select(hr => new HistoryRecordDto(
            hr.SongId,
            hr.Song.Title,
            hr.Song.Artist,
            hr.PlayedAt,
            hr.Context
        ));
    }

    public async Task ClearUserHistoryAsync(Guid userId)
    {
        await historyRepository.ClearUserHistoryAsync(userId);
    }
    
    public async Task<IEnumerable<SongDto>> GetLastUniqueSongsAsync(Guid userId, int limit)
    {
        var songIds = await historyRepository.GetLastUniqueSongIdsAsync(userId, limit);
        var songs = await songService.GetSongsByIdsAsync(songIds);

        return songIds
            .Select(id => songs.FirstOrDefault(s => s.Id == id))
            .Where(song => song != null)!;
    }
}