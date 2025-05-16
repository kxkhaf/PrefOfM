using AutoMapper;
using MusicService.Application.DTOs;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;
using MusicService.Domain.Entities;
using MusicService.Domain.Exceptions;

namespace MusicService.Application.Services;

public class PlaylistService(
    IPlaylistRepository playlistRepository,
    ISongRepository songRepository,
    IMapper mapper) : IPlaylistService
{
    public async Task<IEnumerable<PlaylistDto>> GetUserPlaylistsAsync(Guid userId)
    {
        var playlists = await playlistRepository.GetByUserIdAsync(userId);
        return mapper.Map<IEnumerable<PlaylistDto>>(playlists);
    }
    
    public async Task<IEnumerable<PlaylistDto>> GetUserPlaylistsByNameAsync(Guid userId, string name)
    {
        var playlists = await playlistRepository.GetByUserIdAndNameAsync(userId, name);
        return mapper.Map<IEnumerable<PlaylistDto>>(playlists);
    }

    public async Task<PlaylistDto?> GetPlaylistByIdAsync(long id, Guid userId)
    {
        var playlist = await playlistRepository.GetByIdWithSongsAsync(id);
        if (playlist == null || playlist.UserId != userId)
        {
            return null;
        }
        return mapper.Map<PlaylistDto>(playlist);
    }

    public async Task<long> CreatePlaylistAsync(CreatePlaylistDto dto, Guid userId)
    {
        var playlist = new Playlist
        {
            Name = dto.Name,
            Description = dto.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await playlistRepository.AddAsync(playlist);
        return playlist.Id;
    }

    public async Task AddSongToPlaylistAsync(long playlistId, long songId, Guid userId)
    {
        var playlist = await playlistRepository.GetByIdAsync(playlistId);
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", playlistId);
        }

        var song = await songRepository.GetByIdAsync(songId);
        if (song == null)
        {
            throw new NotFoundException("Song", songId);
        }

        await playlistRepository.AddSongToPlaylistAsync(playlistId, songId);
    }

    public async Task AddSongToFavouritesPlaylistAsync(long songId, Guid userId)
    {
        var playlist = await playlistRepository.GetPlayilistDataByUserIdAndNameAsync(userId ,"Favourite");
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", "Favourite");
        }
        
        var song = await songRepository.GetByIdAsync(songId);
        if (song == null)
        {
            throw new NotFoundException("Song", songId);
        }

        await playlistRepository.AddSongToPlaylistAsync(playlist.Id, songId);
    }

    public async Task RemoveSongFromPlaylistAsync(long playlistId, long songId, Guid userId)
    {
        var playlist = await playlistRepository.GetByIdAsync(playlistId);
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", playlistId);
        }

        await playlistRepository.RemoveSongFromPlaylistAsync(playlistId, songId);
    }

    public async Task RemoveSongFromFavouritesPlaylistAsync(long songId, Guid userId)
    {
        var playlist = await playlistRepository.GetPlayilistDataByUserIdAndNameAsync(userId ,"Favourite");
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", "Favourite");
        }

        await playlistRepository.RemoveSongFromPlaylistAsync(playlist.Id, songId);
    }

    public async Task DeletePlaylistAsync(long id, Guid userId)
    {
        var playlist = await playlistRepository.GetByIdAsync(id);
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", id);
        }

        await playlistRepository.DeleteAsync(playlist);
    }

    public async Task UpdatePlaylistInfoAsync(UpdatePlaylistDto dto, long playlistId, Guid userId)
    {
        var playlist = await playlistRepository.GetByIdAsync(playlistId);
    
        if (playlist == null || playlist.UserId != userId)
        {
            throw new NotFoundException("Playlist", playlistId);
        }
        
        var updatedPlaylist = new Playlist
        {
            Id = playlistId,
            Name = dto.Name ?? playlist.Name,
            Description = dto.Description
        };

        await playlistRepository.UpdateAsync(updatedPlaylist);
    }

    public async Task<IEnumerable<PlaylistBasicInfoDto>> GetUserPlaylistsBasicInfoAsync(Guid userId)
    {
        var playlists = await playlistRepository.GetUserPlaylistsBasicInfoAsync(userId);
        return mapper.Map<IEnumerable<PlaylistBasicInfoDto>>(playlists);
    }
}