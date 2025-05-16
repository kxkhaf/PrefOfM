namespace MusicService.Application.DTOs;

public record RedisSongDto(
    long Id,
    string Title,
    string Artist,
    string? RealUrl,
    string LastFmUrl,
    string Emotion);