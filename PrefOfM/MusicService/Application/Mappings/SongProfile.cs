using AutoMapper;
using MusicService.Application.DTOs;
using MusicService.Domain.Entities;

namespace MusicService.Application.Mappings;


public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Song, SongDto>()
            .ForMember(dest => dest.Emotion, opt => opt.MapFrom(src => src.Emotion.ToString()));
        
        CreateMap<PlaylistSong, SongToPlaylistRequest>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Song.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Song.Title))
            .ForMember(dest => dest.Artist, opt => opt.MapFrom(src => src.Song.Artist))
            .ForMember(dest => dest.RealUrl, opt => opt.MapFrom(src => src.Song.RealUrl))
            .ForMember(dest => dest.LastFmUrl, opt => opt.MapFrom(src => src.Song.LastFmUrl))
            .ForMember(dest => dest.Emotion, opt => opt.MapFrom(src => src.Song.Emotion.ToString()))
            .ForMember(dest => dest.AddedAt, opt => opt.MapFrom(src => src.AddedAt));
        
        CreateMap<Playlist, PlaylistDto>()
            .ForMember(dest => dest.Songs, 
                opt => opt.MapFrom(src => src.PlaylistSongs
                    .OrderBy(ps => ps.Position)
                    .Select(ps => ps.Song)));
        
        CreateMap<CreatePlaylistDto, Playlist>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.PlaylistSongs, opt => opt.Ignore());
        
        CreateMap<Playlist, PlaylistBasicInfoDto>();
    }
}