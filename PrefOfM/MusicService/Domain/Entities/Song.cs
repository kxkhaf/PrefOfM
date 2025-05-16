using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MusicService.Application.DTOs;
using MusicService.Domain.Enums;
using NpgsqlTypes;

namespace MusicService.Domain.Entities;

public class Song
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Artist { get; set; } = null!;

    [Url]
    [MaxLength(500)]
    public string? RealUrl { get; set; }

    [Required]
    [Url]
    [MaxLength(500)]
    public string LastFmUrl { get; set; } = null!;
    
    public Emotion Emotion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Column(TypeName = "tsvector")]
    public NpgsqlTsVector? SearchVector { get; set; }

    public ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
    public ICollection<HistoryRecord> HistoryRecords { get; set; } = new List<HistoryRecord>();
}

public class Playlist
{
    public long Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string? Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [NotMapped]
    public int SongCount => PlaylistSongs?.Count ?? 0;
    public ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
}

public class HistoryRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(Song))]
    public long SongId { get; set; }
    public Song Song { get; set; } = null!;

    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public string? Context { get; set; }
}

public class PlaylistSong
{
    [ForeignKey(nameof(Playlist))]
    public long PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;
    
    [ForeignKey(nameof(Song))]
    public long SongId { get; set; }
    public Song Song { get; set; } = null!;
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int? Position { get; set; }
}

public record PlaylistDto
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public IEnumerable<SongDto> Songs { get; init; } = Enumerable.Empty<SongDto>();

    public PlaylistDto() {}
}
    
public record CreatePlaylistDto(
    string Name,
    string? Description);
    
    
public record SongToPlaylistRequest(
    long Id,
    string Title,
    string Artist,
    string? RealUrl,
    string LastFmUrl,
    string Emotion,
    DateTime AddedAt);