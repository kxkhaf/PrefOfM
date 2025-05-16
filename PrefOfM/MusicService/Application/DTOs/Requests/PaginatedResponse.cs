namespace MusicService.Application.DTOs.Requests;

public record PaginatedResponse<T>(
    IEnumerable<T> Data,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages - 1;
    public bool HasPreviousPage => PageNumber > 0;
};