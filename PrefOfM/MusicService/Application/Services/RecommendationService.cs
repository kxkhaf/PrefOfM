using System.Text.Json;
using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Domain.Enums;

namespace MusicService.Application.Services;

public class RecommendationService(
    ISongRepository songRepository,
    IPlaylistRepository playlistRepository,
    IRedisCacheService cache,
    ILogger<RecommendationService> logger)
    : IRecommendationService
{
    private const double PersonalizedWeight = 0.4;
    private const double PopularWeight = 0.3;
    private const double RecentWeight = 0.2;
    private const double RandomWeight = 0.1;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public async Task<IEnumerable<SongDto>> GetRecommendedSongsAsync(
        Guid userId,
        string? query = null,
        int page = 1,
        int limit = 50,
        bool includePersonalized = true,
        bool includePopular = true,
        bool includeRecent = true,
        bool includeRandom = true)
    {
        try
        {
            var cacheKey = GetCacheKey(userId, query, page, limit, 
                includePersonalized, includePopular, includeRecent, includeRandom);

            if (await cache.ExistsAsync(cacheKey))
            {
                var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
                var songs = cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
                return MapToDto(songs);
            }

            var allSongs = await GetAllRecommendedSongsMixed(
                userId,
                query,
                includePersonalized,
                includePopular,
                includeRecent,
                includeRandom);

            var paginatedSongs = allSongs
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();
            
            await cache.SetAsync(cacheKey, paginatedSongs.Select(ToRedisDto).ToList(), _cacheExpiration);

            return MapToDto(paginatedSongs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetRecommendedSongsAsync");
            throw;
        }
    }

    private RedisSongDto ToRedisDto(Song song)
    {
        return new RedisSongDto(
            song.Id,
            song.Title,
            song.Artist,
            song.RealUrl,
            song.LastFmUrl,
            song.Emotion.ToString());
    }

    private Song FromRedisDto(RedisSongDto dto)
    {
        return new Song
        {
            Id = dto.Id,
            Title = dto.Title,
            Artist = dto.Artist,
            RealUrl = dto.RealUrl,
            LastFmUrl = dto.LastFmUrl,
            Emotion = Enum.Parse<Emotion>(dto.Emotion)
        };
    }

    private async Task<List<Song>> GetAllRecommendedSongsMixed(
        Guid userId,
        string? query,
        bool includePersonalized,
        bool includePopular,
        bool includeRecent,
        bool includeRandom)
    {
        var tasks = new List<Task<List<Song>>>();

        if (includePersonalized)
            tasks.Add(GetPersonalizedRecommendations(userId, query));
            
        if (includePopular)
            tasks.Add(GetPopularRecommendations(query));
            
        if (includeRecent)
            tasks.Add(GetRecentRecommendations(userId, query));

        await Task.WhenAll(tasks);

        var allSongs = tasks.SelectMany(t => t.Result)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .ToList();
        
        if (includeRandom)
        {
            var randomSongs = await GetRandomRecommendations(query);
            allSongs = allSongs.Union(randomSongs).ToList();
        }

        return MixRecommendations(
            personalized: includePersonalized ? tasks.ElementAtOrDefault(0)?.Result ?? new() : new(),
            popular: includePopular ? tasks.ElementAtOrDefault(1)?.Result ?? new() : new(),
            recent: includeRecent ? tasks.ElementAtOrDefault(2)?.Result ?? new() : new(),
            random: includeRandom ? await GetRandomRecommendations(query) : new(),
            allSongs: allSongs);
    }

    private async Task<List<Song>> GetPersonalizedRecommendations(Guid userId, string? query)
    {
        var cacheKey = $"recs:personalized:{userId}:{query}";
        if (await cache.ExistsAsync(cacheKey))
        {
            var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
            return cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
        }

        // Favorite
        var favoriteSongIds = (await playlistRepository.GetByUserIdAsync(userId))
            .SelectMany(p => p.PlaylistSongs)
            .Select(ps => ps.SongId)
            .Distinct()
            .ToList();

        if (!favoriteSongIds.Any())
            return new List<Song>();

        // Поиск пользователей с похожими вкусами
        var similarUsers = (await playlistRepository.GetUsersWithSimilarTastes(favoriteSongIds, userId))
            .Distinct()
            .ToList();

        // Получение рекомендаций на основе песен пользователей с похожими вкусами
        var recommendedSongs = await songRepository.GetRecommendedByUsersAsync(similarUsers, favoriteSongIds, query);

        await cache.SetAsync(cacheKey, recommendedSongs.Select(ToRedisDto).ToList(), _cacheExpiration);
        return recommendedSongs;
    }

    private async Task<List<Song>> GetPopularRecommendations(string? query)
    {
        var cacheKey = $"recs:popular:{query}";
        if (await cache.ExistsAsync(cacheKey))
        {
            var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
            return cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
        }

        var popularSongs = await songRepository.GetPopularSongsAsync(query);
        await cache.SetAsync(cacheKey, popularSongs.Select(ToRedisDto).ToList(), _cacheExpiration);
        return popularSongs;
    }

    private async Task<List<Song>> GetRecentRecommendations(Guid userId, string? query)
    {
        var cacheKey = $"recs:recent:{userId}:{query}";
        if (await cache.ExistsAsync(cacheKey))
        {
            var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
            return cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
        }

        var recentSongs = await songRepository.GetRecentlyListenedRecommendationsAsync(userId, query);
        await cache.SetAsync(cacheKey, recentSongs.Select(ToRedisDto).ToList(), _cacheExpiration);
        return recentSongs;
    }

    private async Task<List<Song>> GetRandomRecommendations(string? query)
    {
        var cacheKey = $"recs:random:{query}";
        if (await cache.ExistsAsync(cacheKey))
        {
            var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
            return cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
        }

        var randomSongs = await songRepository.GetRandomSongsAsync(50, query);
        await cache.SetAsync(cacheKey, randomSongs.Select(ToRedisDto).ToList(), _cacheExpiration);
        return randomSongs;
    }

    private List<Song> MixRecommendations(
        List<Song> personalized,
        List<Song> popular,
        List<Song> recent,
        List<Song> random,
        List<Song> allSongs)
    {
        var songScores = new Dictionary<long, double>();

        AddScores(songScores, personalized, PersonalizedWeight);
        AddScores(songScores, popular, PopularWeight);
        AddScores(songScores, recent, RecentWeight);
        AddScores(songScores, random, RandomWeight);

        return allSongs
            .OrderByDescending(s => songScores.GetValueOrDefault(s.Id, 0))
            .ThenBy(_ => Guid.NewGuid())
            .ToList();
    }

    private void AddScores(Dictionary<long, double> songScores, List<Song> songs, double weight)
    {
        for (int i = 0; i < songs.Count; i++)
        {
            var score = weight * (1 - (double) i / songs.Count);
            if (!songScores.TryAdd(songs[i].Id, score))
                songScores[songs[i].Id] += score;
        }
    }
    
    public async Task InvalidateRecommendationCacheAsync(Guid userId)
    {
        await cache.DeleteByPatternAsync($"recs:*:{userId}:*");
        await cache.DeleteByPatternAsync($"recs:personalized:{userId}:*");
        await cache.DeleteByPatternAsync($"recs:recent:{userId}:*");
        await cache.DeleteByPatternAsync($"recs:random:{userId}:*");
    }

    private static string GetCacheKey(
        Guid userId,
        string? query,
        int page,
        int limit,
        bool includePersonalized,
        bool includePopular,
        bool includeRecent,
        bool includeRandom)
    {
        return $"recs:full:{userId}:{query}:{page}:{limit}:{includePersonalized}:{includePopular}:{includeRecent}:{includeRandom}";
    }
    
public async Task<IEnumerable<SongDto>> GetRecommendedSongsByEmotionAsync(
    Guid userId,
    Emotion emotion,
    string? query = null,
    int page = 1,
    int limit = 50,
    bool includePersonalized = true,
    bool includePopular = true,
    bool includeRecent = true,
    bool includeRandom = true)
{
    try
    {
        var cacheKey = GetEmotionCacheKey(userId, emotion, query, page, limit, 
            includePersonalized, includePopular, includeRecent, includeRandom);

        if (await cache.ExistsAsync(cacheKey))
        {
            var cachedDtos = await cache.GetAsync<List<RedisSongDto>>(cacheKey);
            var songs = cachedDtos?.Select(FromRedisDto).ToList() ?? new List<Song>();
            return MapToDto(songs);
        }

        // Получаем больше песен, чтобы после фильтрции осталось достаточно
        var initialLimit = Math.Max(limit * 6, 300);
        var allSongs = await GetAllRecommendedSongsMixed(
            userId,
            query,
            includePersonalized,
            includePopular,
            includeRecent,
            includeRandom);

        // Фильтруем по эмоции и берем нужное количество
        var filteredByEmotion = allSongs
            .Where(s => s.Emotion == emotion)
            .Take(initialLimit)
            .ToList();

        // Если после фильтрации осталось мало песен, добавляем случайные с нужной эмоцией
        if (filteredByEmotion.Count < limit)
        {
            var additionalRandomSongs = (await songRepository.GetRandomSongsByEmotionAsync(
                limit - filteredByEmotion.Count, 
                emotion, 
                query))
                .Where(s => filteredByEmotion.All(fs => fs.Id != s.Id))
                .ToList();

            filteredByEmotion.AddRange(additionalRandomSongs);
        }

        var paginatedSongs = filteredByEmotion
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        await cache.SetAsync(cacheKey, paginatedSongs.Select(ToRedisDto).ToList(), _cacheExpiration);

        return MapToDto(paginatedSongs);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in GetRecommendedSongsByEmotionAsync");
        throw;
    }
}

    private static string GetEmotionCacheKey(
        Guid userId,
        Emotion emotion,
        string? query,
        int page,
        int limit,
        bool includePersonalized,
        bool includePopular,
        bool includeRecent,
        bool includeRandom)
    {
        return $"recs:emotion:{userId}:{emotion}:{query}:{page}:{limit}:{includePersonalized}:{includePopular}:{includeRecent}:{includeRandom}";
    }
    
    private static IEnumerable<SongDto> MapToDto(IEnumerable<Song> songs)
    {
        return songs.Select(song => new SongDto(
            song.Id,
            song.Title,
            song.Artist,
            song.RealUrl,
            song.LastFmUrl,
            song.Emotion.ToString()));
    }
}

public interface IRecommendationService
{
    Task<IEnumerable<SongDto>> GetRecommendedSongsAsync(
        Guid userId,
        string? query = null,
        int page = 1,
        int limit = 50,
        bool includePersonalized = true,
        bool includePopular = true,
        bool includeRecent = true,
        bool includeRandom = true);

    Task<IEnumerable<SongDto>> GetRecommendedSongsByEmotionAsync(
        Guid userId,
        Emotion emotion,
        string? query = null,
        int page = 1,
        int limit = 50,
        bool includePersonalized = true,
        bool includePopular = true,
        bool includeRecent = true,
        bool includeRandom = true);

    Task InvalidateRecommendationCacheAsync(Guid userId);
}