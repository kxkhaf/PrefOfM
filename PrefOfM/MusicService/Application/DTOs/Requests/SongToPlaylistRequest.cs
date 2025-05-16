namespace MusicService.Application.DTOs.Requests;

public record SongToPlaylistRequest(
    long Id,
    string Title,
    string Artist,
    string? RealUrl,
    string LastFmUrl,
    string Emotion,
    DateTime AddedAt);