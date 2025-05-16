using System.ComponentModel.DataAnnotations;

namespace MusicService.Application.DTOs;

public class UpdatePlaylistDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}