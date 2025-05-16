using Microsoft.EntityFrameworkCore;
using MusicService.Application.DTOs.Requests;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Domain.Enums;
using MusicService.Infrastructure.Data;

namespace MusicService.Infrastructure.Repositories;

public class SongRepository(ApplicationDbContext context) : ISongRepository
{
    public async Task<Song?> GetByIdAsync(long id)
    {
        return await context.Songs.FindAsync(id);
    }

public async Task<List<Song>> SearchSongsAsync(string query, int pageNum, int limit = 50)
    {
    if (string.IsNullOrWhiteSpace(query))
        return new List<Song>();

    // Защита от некорректных значений
    pageNum = Math.Max(1, pageNum);
    limit = Math.Max(1, Math.Min(limit, 1000));
    var offset = (pageNum - 1) * limit;
    
    if (Enum.TryParse<Emotion>(query.Replace(" ", ":"), ignoreCase: true, out var parsedEmotion))
    {
        var emotionMatches = await context.Songs
            .Where(s => s.Emotion == parsedEmotion)
            .OrderByDescending(s => s.Id)
            .Skip(offset)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        if (emotionMatches.Any())
            return emotionMatches;
    }
    
    query = query.Trim();
    
    // Подготовка шаблона для частичного совпадения
    var likePattern = $"%{query.ToLower()}%";

    var a = await context.Songs
        .FromSqlInterpolated($"""
            WITH search_query AS (
                SELECT 
                    websearch_to_tsquery('english', {query}) AS english_query,
                    websearch_to_tsquery('russian', {query}) AS russian_query,
                    plainto_tsquery('simple', {query}) AS simple_query,
                    {likePattern} AS like_pattern
            ),
            ranked_songs AS (
                SELECT 
                    s.*,
                    -- Общий рейтинг релевантности
                    (
                        -- Полнотекстовый поиск (высший приоритет)
                        CASE WHEN s."SearchVector" @@ COALESCE(english_query, russian_query, simple_query) 
                             THEN 1000 ELSE 0 END +
                        
                        -- Точное совпадение Emotion (высокий приоритет)
                        CASE WHEN LOWER("Emotion"::text) = LOWER({query}) THEN 500 ELSE 0 END +
                        
                        -- Частичное совпадение в Title (средний приоритет)
                        CASE WHEN LOWER("Title") LIKE like_pattern THEN 300 ELSE 0 END +
                        
                        -- Частичное совпадение в Artist (низкий приоритет)
                        CASE WHEN LOWER("Artist") LIKE like_pattern THEN 200 ELSE 0 END +
                        
                        -- Частичное совпадение в Emotion (самый низкий приоритет)
                        CASE WHEN LOWER("Emotion"::text) LIKE like_pattern THEN 100 ELSE 0 END +
                        
                        -- Рейтинг полнотекстового поиска (уточнение среди результатов)
                        ts_rank(to_tsvector('simple', "Title"), COALESCE(english_query, russian_query, simple_query)) * 2 +
                        ts_rank(to_tsvector('simple', "Artist"), COALESCE(english_query, russian_query, simple_query))
                    ) AS relevance_score
                FROM 
                    "Songs" s,
                    search_query
                WHERE 
                    s."SearchVector" @@ COALESCE(english_query, russian_query, simple_query)
                    OR LOWER("Emotion"::text) = LOWER({query})
                    OR LOWER("Title") LIKE like_pattern
                    OR LOWER("Artist") LIKE like_pattern
                    OR LOWER("Emotion"::text) LIKE like_pattern
            )
            SELECT * FROM ranked_songs
            ORDER BY 
                relevance_score DESC
            LIMIT {limit}
            OFFSET {offset}
            """)
        .AsNoTracking()
        .ToListAsync();
    return a;
    }

    public async Task<IEnumerable<Song>> GetByIdsAsync(IEnumerable<long> ids)
    {
        return await context.Songs
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Song>> GetByEmotionAsync(Emotion emotion, int limit)
    {
        return await context.Songs
            .Where(s => s.Emotion == emotion)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Song>> GetAllAsync()
    {
        return await context.Songs.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Song>> GetSongsAsync(int pageNum, int limit)
    {
        pageNum = Math.Max(1, pageNum);
        limit = Math.Max(1, Math.Min(limit, 50));

        return await context.Songs
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((pageNum - 1) * limit)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }


    public async Task<IEnumerable<Song>> GetByEmotionAsync(Emotion emotion)
    {
        return await context.Songs
            .Where(s => s.Emotion == emotion)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Song song)
    {
        await context.Songs.AddAsync(song);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Song song)
    {
        context.Songs.Update(song);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Song song)
    {
        context.Songs.Remove(song);
        await context.SaveChangesAsync();
    }
    
    private IQueryable<Song> ApplySearchQuery(IQueryable<Song> query, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery)) 
            return query;

        return query.Where(s => 
            EF.Functions.ILike(s.Title, $"%{searchQuery}%") ||
            EF.Functions.ILike(s.Artist, $"%{searchQuery}%") ||
            EF.Functions.ILike(s.Emotion.ToString(), $"%{searchQuery}%"));
    }
    
    public async Task<List<Song>> GetRecommendedByUsersAsync(
        List<Guid> similarUserIds,
        List<long> excludeSongIds,
        string? query = null)
    {
        var baseQuery = ApplySearchQuery(context.Songs, query);

        return await baseQuery
            .Where(s => context.PlaylistSongs
                .Any(ps => similarUserIds.Contains(ps.Playlist.UserId) && 
                           ps.SongId == s.Id &&
                           !excludeSongIds.Contains(s.Id)))
            .GroupBy(s => s.Id)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .Take(500)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Song>> GetPopularSongsAsync(string? query = null)
    {
        var baseQuery = ApplySearchQuery(context.Songs, query);

        return await baseQuery
            .OrderByDescending(s => s.PlaylistSongs.Count())
            .ThenByDescending(s => s.HistoryRecords.Count())
            .Take(500)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Song>> GetRecentlyListenedRecommendationsAsync(
        Guid userId,
        string? query = null)
    {
        var baseQuery = ApplySearchQuery(context.Songs, query);

        var recentSongIds = await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .OrderByDescending(hr => hr.PlayedAt)
            .Select(hr => hr.SongId)
            .Distinct()
            .Take(5)
            .ToListAsync();

        if (!recentSongIds.Any())
            return new List<Song>();

        var recentSongs = await context.Songs
            .Where(s => recentSongIds.Contains(s.Id))
            .ToListAsync();

        var recentArtists = recentSongs.Select(s => s.Artist).Distinct();
        var recentEmotions = recentSongs.Select(s => s.Emotion).Distinct();

        return await baseQuery
            .Where(s => recentArtists.Contains(s.Artist) || 
                        recentEmotions.Contains(s.Emotion))
            .OrderByDescending(s => s.CreatedAt)
            .Take(500)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetTotalSongCountAsync()
    {
        return await context.Songs.CountAsync();
    }

    public async Task<List<Song>> GetRandomSongsAsync(int limit, string? query = null)
    {
        var queryable = context.Songs.AsQueryable();
    
        if (!string.IsNullOrEmpty(query))
        {
            queryable = queryable.Where(s => s.Title.Contains(query) || s.Artist.Contains(query));
        }

        return await queryable
            .OrderBy(x => EF.Functions.Random())
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Song>> GetRandomSongsByEmotionAsync(int count, Emotion emotion, string? query = null)
    {
        count = Math.Max(1, Math.Min(count, 1000));

        // Получаем общее количество подходящих песен
        var totalCount = await context.Songs
            .Where(s => s.Emotion == emotion)
            .CountAsync();

        if (totalCount == 0)
        {
            return new List<Song>();
        }

        // Если запрашиваем больше половины доступных, проще взять все и выбрать случайные в памяти
        if (count > totalCount / 2)
        {
            var allSongs = await context.Songs
                .Where(s => s.Emotion == emotion)
                .AsNoTracking()
                .ToListAsync();

            return allSongs
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToList();
        }
        var randomSongs = new List<Song>();
        var attempts = 0;
        const int maxAttempts = 3;

        while (randomSongs.Count < count && attempts < maxAttempts)
        {
            var needed = count - randomSongs.Count;
            var batch = await context.Songs
                .Where(s => s.Emotion == emotion)
                .OrderBy(x => EF.Functions.Random())
                .Take(needed)
                .AsNoTracking()
                .ToListAsync();

            randomSongs.AddRange(batch);
            attempts++;
        }

        return randomSongs.Take(count).ToList();
    }
}