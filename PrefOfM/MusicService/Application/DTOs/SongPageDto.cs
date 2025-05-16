namespace MusicService.Application.DTOs;

public class SongPageDto
{
    public required long[] ExcludeSongIds { get; set; }
    public int PageSize { get; set; } = 20;
    public bool IncludeTotalCount { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}