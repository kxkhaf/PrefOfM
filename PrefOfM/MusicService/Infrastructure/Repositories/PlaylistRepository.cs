using Microsoft.EntityFrameworkCore;
using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Domain.Exceptions;
using MusicService.Infrastructure.Data;

namespace MusicService.Infrastructure.Repositories;

public class PlaylistRepository(ApplicationDbContext context) : IPlaylistRepository
{
    public async Task<Playlist?> GetByIdAsync(long id)
    {
        return await context.Playlists.FindAsync(id);
    }

    public async Task<Playlist?> GetByNameAsync(string name)
    {
        return await context.Playlists.Where(x => x.Name == name).FirstOrDefaultAsync();
    }

    public async Task<Playlist?> GetByIdWithSongsAsync(long id)
    {
        return await context.Playlists
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Playlist> GetPlayilistDataByUserIdAndNameAsync(Guid userId, string name)
    {
        return await context.Playlists
            .Where(p => p.UserId == userId)
            .Where(p => p.Name == name)
            .FirstAsync();
    }

    public async Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId)
    {
        return await context.Playlists
            .Where(p => p.UserId == userId)
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Playlist>> GetByUserIdAndNameAsync(Guid userId, string name)
    {
        return await context.Playlists
            .Where(p => p.UserId == userId)
            .Where(p => p.Name == name)
            .Include(p => p.PlaylistSongs)
            .ThenInclude(ps => ps.Song)
            .ToListAsync();
    }

    public async Task AddAsync(Playlist playlist)
    {
        await context.Playlists.AddAsync(playlist);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Playlist playlist)
    {
        var existingPlaylist = await context.Playlists
            .FirstOrDefaultAsync(p => p.Id == playlist.Id);

        if (existingPlaylist == null)
        {
            throw new NotFoundException("Playlist", playlist.Id);
        }

        if (playlist.Name != null)
        {
            existingPlaylist.Name = playlist.Name;
        }
        existingPlaylist.Description = playlist.Description;
        
        existingPlaylist.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Playlist playlist)
    {
        context.Playlists.Remove(playlist);
        await context.SaveChangesAsync();
    }

    public async Task AddSongToPlaylistAsync(long playlistId, long songId)
    {
        var exists = await context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);
        if (exists) return;

        var playlistSong = new PlaylistSong
        {
            PlaylistId = playlistId,
            SongId = songId,
            AddedAt = DateTime.UtcNow
        };

        await context.PlaylistSongs.AddAsync(playlistSong);
        await context.SaveChangesAsync();
    }

    public async Task RemoveSongFromPlaylistAsync(long playlistId, long songId)
    {
        var playlistSong = await context.PlaylistSongs
            .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

        if (playlistSong != null)
        {
            context.PlaylistSongs.Remove(playlistSong);
            await context.SaveChangesAsync();
        }
    }
    public async Task<IEnumerable<Playlist>> GetUserPlaylistsBasicInfoAsync(Guid userId)
    {
        return await context.Playlists
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Guid>> GetUsersWithSimilarTastes(List<long> songIds, Guid excludeUserId)
    {
        return await context.PlaylistSongs
            .Where(ps => songIds.Contains(ps.SongId) && ps.Playlist.UserId != excludeUserId)
            .Select(ps => ps.Playlist.UserId)
            .Distinct()
            .Take(100) // Ограничиваем количество похожих пользователей
            .ToListAsync();
    }
}
