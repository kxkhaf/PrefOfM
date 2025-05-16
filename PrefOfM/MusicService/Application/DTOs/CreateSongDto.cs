namespace MusicService.Application.DTOs;

public record CreateSongDto(
    string Title,
    string Artist,
    string? RealUrl,
    string LastFmUrl,
    string Emotion);