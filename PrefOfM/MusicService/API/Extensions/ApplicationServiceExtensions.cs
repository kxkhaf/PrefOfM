using Microsoft.Extensions.Options;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Services;
using MusicService.Application.Settings;
using MusicService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace MusicService.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISongService, SongService>();
        services.AddScoped<IPlaylistService, PlaylistService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IHistoryRecordService, HistoryRecordService>();
        services.AddScoped<IHistoryReaderService, HistoryReaderService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<ISongService, SongService>();
        
        return services;
    }
}