using System.ComponentModel.DataAnnotations;

namespace MusicService.Application.DTOs;

public record AddHistoryRecordDto(
    [Range(1, long.MaxValue)] long SongId,
    [MaxLength(50)] string? Context);