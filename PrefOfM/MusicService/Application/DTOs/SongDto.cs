namespace MusicService.Application.DTOs;

public record SongDto(
    long Id,
    string Title,
    string Artist,
    string? RealUrl,
    string LastFmUrl,
    string Emotion)
{
    public SongDto() : this(0, string.Empty, string.Empty, null, string.Empty, string.Empty) {}
}