using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;
using MusicService.Domain.Entities;
using MusicService.Domain.Enums;
using MusicService.Application.DTOs;
using MusicService.Application.DTOs.Requests;
using MusicService.Domain.Extensions;

namespace MusicService.Application.Services;

public class SongService(ISongRepository repository, IHistoryReaderService historyReaderService) : ISongService
{
    public async Task<IEnumerable<SongDto>> GetAllSongsAsync()
    {
        var songs = await repository.GetAllAsync();
        return songs.Select(MapToDto);
    }

    public async Task<IEnumerable<SongDto>> GetSongsAsync(int pageNum, int limit = 50)
    {
        var songs = await repository.GetSongsAsync(pageNum,limit);
        return songs.Select(MapToDto);
    }

    public async Task<SongDto?> GetSongByIdAsync(long id)
    {
        var song = await repository.GetByIdAsync(id);
        return song is null ? null : MapToDto(song);
    }

    public async Task<IEnumerable<SongDto>> GetSongsByEmotionAsync(string emotion, int limit = 50)
    {
        if (!Enum.TryParse<Emotion>(emotion, true, out var parsedEmotion))
            return [];
        parsedEmotion = parsedEmotion is Emotion.Other ? parsedEmotion.GetRandomEmotion() : parsedEmotion;
        var songs = await repository.GetByEmotionAsync(parsedEmotion, limit);
        return songs.Select(MapToDto);
    }

    public async Task<long> CreateSongAsync(CreateSongDto dto)
    {
        if (!Enum.TryParse<Emotion>(dto.Emotion, true, out var parsedEmotion))
            throw new ArgumentException("Invalid emotion value", nameof(dto.Emotion));

        var song = new Song
        {
            Title = dto.Title,
            Artist = dto.Artist,
            RealUrl = dto.RealUrl,
            Emotion = parsedEmotion
        };

        await repository.AddAsync(song);
        return song.Id;
    }
    public async Task<List<SongDto>> SearchSongsAsync(string query, int pageNum, int limit = 100)
    {
        var songs = await repository.SearchSongsAsync(query, pageNum, limit);
        return songs.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<SongDto>> GetSongsByIdsAsync(IEnumerable<long> ids)
    {
        var uniqueIds = ids.Distinct().ToList();

        if (!uniqueIds.Any())
            return [];
        
        var songs = await repository.GetByIdsAsync(uniqueIds);
        var songDictionary = songs.ToDictionary(s => s.Id);
        return uniqueIds
            .Where(id => songDictionary.ContainsKey(id))
            .Select(id => MapToDto(songDictionary[id]));
    }
    
    public async Task<IEnumerable<SongDto>> GetRecentlyPlayedSongsAsync(Guid userId, int limit)
    {
        // 1. Получаем историю прослушиваний
        var history = await historyReaderService.GetRecentHistoryAsync(userId, limit);
    
        // 2. Извлекаем ID песен, сохраняя порядок
        var orderedSongIds = history
            .Select(r => r.SongId)
            .Distinct()
            .ToList();

        if (!orderedSongIds.Any())
            return [];

        // 3. Получаем данные песен
        var songs = await repository.GetByIdsAsync(orderedSongIds);
        var songDict = songs.ToDictionary(s => s.Id);
        
        return history
            .Where(r => songDict.ContainsKey(r.SongId))
            .Select(r => 
            {
                var song = songDict[r.SongId];
                return new SongDto(
                    Id: song.Id,
                    Title: song.Title,
                    Artist: song.Artist,
                    RealUrl: song.RealUrl,
                    LastFmUrl: song.LastFmUrl,
                    Emotion: song.Emotion.ToString()
                );
            });
    }
    
    public async Task<PaginatedResponse<SongDto>> GetPaginatedSongsAsync(int pageNum, int limit)
    {
        try
        {
            var songs = await repository.GetSongsAsync(pageNum, limit);
            var totalCount = await repository.GetTotalSongCountAsync();

            var songDtos = songs.Select(s => new SongDto(
                s.Id,
                s.Title,
                s.Artist,
                s.RealUrl,
                s.LastFmUrl,
                s.Emotion.ToString()
            ));

            return new PaginatedResponse<SongDto>(
                Data: songDtos,
                PageNumber: pageNum,
                PageSize: limit,
                TotalCount: totalCount
            );
        }
        catch (Exception ex)
        {
            throw new ApplicationException(ex.Message);
        }
    }

    public async Task<int> GetTotalSongCountAsync()
    {
        return await repository.GetTotalSongCountAsync();
    }

    private static SongDto MapToDto(Song song)
    {
        try
        {
            return new(song.Id, song.Title, song.Artist, song.RealUrl, song.LastFmUrl, song.Emotion.ToString());
        }
        catch (Exception e)
        {
            throw new ApplicationException("Could not map song", e);
        }
    }
}