using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Mappings;
using MusicService.Application.Services;
using MusicService.Application.Settings;
using MusicService.Constants;
using MusicService.Infrastructure.Data;
using MusicService.Infrastructure.Services;
using MusicService.Services.Implementation;
using StackExchange.Redis;

namespace MusicService.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddSwagger()
            .AddConfiguration()
            .AddRedis()
            .AddDatabase()
            .AddAuthenticationDependencies()
            .AddAuthentication()
            .AddCors()
            .AddApplicationServices()
            .AddNeuroModel()
            .AddAutoMapper();

        return builder;
    }

    private static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisSettings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return ConnectionMultiplexer.Connect(redisSettings.ConnectionString);
        });
        builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

        builder.Services.AddAutoMapper(typeof(ApplicationServiceExtensions).Assembly);
        return builder;
    }

    private static WebApplicationBuilder AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() {Title = "Music Service API", Version = "v1"});
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new()
            {
                {
                    new() {Reference = new() {Type = ReferenceType.SecurityScheme, Id = "Bearer"}},
                    Array.Empty<string>()
                }
            });
        });
        return builder;
    }

    private static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(nameof(DatabaseSettings)));
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
        builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(nameof(CorsSettings)));
        builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(nameof(AuthSettings)));
        builder.Services.Configure<DbSeeds>(builder.Configuration.GetSection(nameof(DbSeeds)));
        builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(nameof(RedisSettings)));
        return builder;
    }

    private static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                builder.Configuration.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>()!.ConnectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });
        return builder;
    }

    private static WebApplicationBuilder AddAuthenticationDependencies(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IJwtKeyManager, JwtKeyManager>();
        builder.Services.AddHostedService<JwtKeyRefreshService>();
        return builder;
    }

    private static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()!;
                
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    TryAllIssuerSigningKeys = false,
                    ConfigurationManager = null,

                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        var keyManager = builder.Services.BuildServiceProvider().GetRequiredService<IJwtKeyManager>();
                        var key = keyManager.GetSigningKeyAsync().Result;
                        return [key];
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.OnStarting(async () =>
                        {
                            var keyManager = context.HttpContext.RequestServices
                                .GetRequiredService<IJwtKeyManager>();
                            await keyManager.GetPublicKeyAsync();
                        });
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    private static WebApplicationBuilder AddCors(this WebApplicationBuilder builder)
    {
        var corsSettings = builder.Configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>()!;
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                    .WithMethods(corsSettings.AllowedMethods)
                    .AllowCredentials()
                    .AllowAnyHeader();
            });
        });
        return builder;
    }

    private static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IJwtRequestReader, JwtRequestReader>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices();
        return builder;
    }

    private static WebApplicationBuilder AddNeuroModel(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<NeuroModelSettings>(
            builder.Configuration.GetSection(nameof(NeuroModelSettings)));

        builder.Services.AddSingleton<EmotionPredictorService>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<NeuroModelSettings>>().Value;
            return new EmotionPredictorService(settings.ModelPath, settings.LabelPath);
        });

        return builder;
    }

    private static WebApplicationBuilder AddAutoMapper(this WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(typeof(MappingProfiles));
        return builder;
    }
}