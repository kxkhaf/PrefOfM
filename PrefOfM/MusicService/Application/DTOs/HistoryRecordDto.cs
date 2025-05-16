namespace MusicService.Application.DTOs;

public record HistoryRecordDto(
    long SongId,
    string SongTitle,
    string Artist,
    DateTime PlayedAt,
    string? Context);