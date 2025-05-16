namespace MusicService.Application.DTOs;

public record ProfileDto(
    Guid UserId,
    string Username,
    string Email,
    DateTime? Birthday,
    int SongsPlayed,
    int PlaylistsCount,
    int FavoritesCount);