using Microsoft.EntityFrameworkCore;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Data;

namespace MusicService.Infrastructure.Repositories;


public class HistoryRecordRepository(ApplicationDbContext context) : IHistoryRecordRepository
{
    public async Task AddAsync(HistoryRecord record)
    {
        await context.HistoryRecords.AddAsync(record);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<HistoryRecord>> GetUserHistoryAsync(Guid userId, int limit = 50)
    {
        return await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .Include(hr => hr.Song)
            .OrderByDescending(hr => hr.PlayedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ClearUserHistoryAsync(Guid userId)
    {
        var records = await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .ToListAsync();
        
        context.HistoryRecords.RemoveRange(records);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<long>> GetLastUniqueSongIdsAsync(Guid userId, int limit)
    {
        return await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .OrderByDescending(hr => hr.Id) // Сортируем по ID записи (последние добавленные)
            .Select(hr => hr.SongId)
            .Distinct() // Убираем дубликаты SongId
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<HistoryRecord>> GetRecentRecordsAsync(Guid userId, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);
    
        return await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .OrderByDescending(hr => hr.PlayedAt)
            .Take(limit)
            .Include(hr => hr.Song)
            .AsNoTracking()
            .ToListAsync();
    }
}