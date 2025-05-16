namespace MusicService.Application.DTOs.Requests;

public record SongRequest(
    int PageNum = 0,
    int Limit = 50
);