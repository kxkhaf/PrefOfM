namespace MusicService.Application.DTOs.Requests;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 50;
    public int PageNum { get; set; }
}
