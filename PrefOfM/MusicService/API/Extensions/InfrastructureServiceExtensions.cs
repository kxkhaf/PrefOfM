using Microsoft.Extensions.DependencyInjection;
using MusicService.Application.Interfaces.Repositories;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Services;
using MusicService.Infrastructure.Identity;
using MusicService.Infrastructure.Repositories;
using MusicService.Infrastructure.Services;

namespace MusicService.API.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Регистрация репозиториев
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<IPlaylistRepository, PlaylistRepository>();
        services.AddScoped<IHistoryRecordRepository, HistoryRecordRepository>();
        
        
        // Регистрация сервисов инфраструктуры
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        return services;
    }
}