using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Services;
using MusicService.Domain.Exceptions;
using MusicService.Infrastructure.Data;
using Npgsql;

namespace MusicService.Application.Services;

public class ProfileService(ApplicationDbContext context) : IProfileService
{
    public async Task<ProfileDto> GetUserProfileAsync(Guid userId)
    {
        var user = await context.Database.SqlQueryRaw<RawUserData>(
                @$"SELECT 
                    ""Id"" AS ""UserId"",
                    ""UserName"" AS ""Username"",
                    ""Email"",
                    ""Birthday""
                    FROM ""ApplicationUsers""
                    WHERE ""Id"" = @p0",
                new NpgsqlParameter("p0", userId)) //НУЖНО!
            .FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundException("User not found", nameof(user));


        var songsPlayed = await context.HistoryRecords
            .Where(hr => hr.UserId == userId)
            .Select(hr => hr.SongId)
            .Distinct()
            .CountAsync();

        var playlistsCount = await context.Playlists
            .Where(p => p.UserId == userId)
            .CountAsync();

        var favoritesCount = await context.Playlists
            .Where(p => p.UserId == userId && p.Name == "Favourite")
            .SelectMany(p => p.PlaylistSongs)
            .CountAsync();

        return new ProfileDto(
            UserId: user.UserId,
            Username: user.Username,
            Email: user.Email,
            Birthday: user.Birthday,
            SongsPlayed: songsPlayed,
            PlaylistsCount: playlistsCount,
            FavoritesCount: favoritesCount
        );
    }

    private class RawUserData
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime? Birthday { get; set; }
    }
}