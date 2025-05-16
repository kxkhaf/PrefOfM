using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicService.Application.DTOs;
using MusicService.Application.DTOs.Requests;
using MusicService.Application.Interfaces.Services;
using MusicService.Application.Services;
using MusicService.Application.Settings;
using MusicService.Domain.Entities;
using MusicService.Domain.Enums;
using MusicService.Domain.Exceptions;
using MusicService.Domain.Extensions;
using MusicService.Infrastructure.Data;
using MusicService.Infrastructure.Identity;
using MusicService.Services.Implementation;
using Exception = System.Exception;

namespace MusicService.API.Endpoints;

public static class SongEndpoints
{
    public static void MapSongEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "MUSIC Service!").AllowAnonymous();

        app.MapGet("/login", () => "entered");

        app.MapPost("/api/song-by-emotion",
                async (HttpContext context, ISongService songService, EmotionPredictorService emotionService,
                    IRecommendationService recommendationService, IJwtRequestReader jwtReader,
                    ILogger<Program> logger) =>
                {
                    if (!context.Request.HasFormContentType)
                        return Results.BadRequest("Expected multipart/form-data");

                    var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
                    if (!success || userId == null)
                    {
                        return string.IsNullOrEmpty(error)
                            ? Results.Unauthorized()
                            : Results.BadRequest(new {Error = error});
                    }

                    try
                    {
                        var formOptions = new FormOptions
                        {
                            MemoryBufferThreshold = 1024 * 1024 * 100,
                            BufferBody = true
                        };

                        var form = await context.Request.ReadFormAsync(formOptions);
                        var file = form.Files["file"];

                        if (file == null || file.Length == 0)
                            return Results.BadRequest("File is required");

                        if (!file.ContentType.StartsWith("audio/") &&
                            !file.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                            return Results.BadRequest("Only audio files are supported");

                        try
                        {
                            var emotion = await emotionService.PredictEmotionAsync(file);
                            logger.LogInformation($"User: {userId} - {emotion}");
                            if (Enum.TryParse<Emotion>(emotion, true, out var parsedEmotion))
                                parsedEmotion = parsedEmotion is Emotion.Other
                                    ? parsedEmotion.GetRandomEmotion()
                                    : parsedEmotion;

                            var songs = await recommendationService.GetRecommendedSongsByEmotionAsync(userId.Value,
                                parsedEmotion);
                            return Results.Ok(songs);
                        }
                        catch
                        {
                            try
                            {
                                var songs = await songService.GetSongsByEmotionAsync(Emotion.Other.GetRandomEmotion()
                                    .ToString().ToLower());
                                return Results.Ok(songs);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(e, "Error occured while getting recommended songs");
                                return Results.Problem("Error occured while getting recommended songs");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing audio file");
                        return Results.Problem("Error processing audio file");
                    }
                })
            .WithOpenApi()
            .DisableAntiforgery();

        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("→ {Method} {Path}", context.Request.Method, context.Request.Path);

            await next();

            logger.LogInformation("← {Method} {Path} → {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        });

        var groupSongs = app.MapGroup("/api/songs").WithTags("Songs");
        var groupPlaylists = app.MapGroup("/api/playlists").WithTags("Playlists");
        var groupHistory = app.MapGroup("/api/history").WithTags("History").RequireAuthorization();
        var groupProfile = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();
        var groupDb = app.MapGroup("/api/db").WithTags("Database");

        groupSongs.MapGet("/", async (ISongService songService) =>
        {
            var songs = await songService.GetAllSongsAsync();
            return Results.Ok(songs);
        });

        groupSongs.MapPost("/main", async (
            HttpContext context,
            IJwtRequestReader jwtReader,
            IRecommendationService recommendationService,
            ISongService songService,
            ILogger<Program> logger,
            SongRequest request) =>
        {
            try
            {
                var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
                if (!success || userId == null)
                {
                    return string.IsNullOrEmpty(error)
                        ? Results.Unauthorized()
                        : Results.BadRequest(new {Error = error});
                }

                var songs = await recommendationService.GetRecommendedSongsAsync(
                    userId: userId.Value,
                    query: null,
                    page: request.PageNum,
                    limit: request.Limit);

                if (songs is null || !songs.Any())
                    throw new Exception("Preferenses not found");
                return Results.Ok(songs);
            }
            catch (Exception ex)
            {
                try
                {
                    var songs = await songService.GetSongsAsync(
                        pageNum: request!.PageNum,
                        limit: request.Limit
                    );

                    return Results.Ok(songs);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error in songs/main endpoint");
                    return Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "An error occurred while processing your request");
                }
            }
        });

        groupSongs.MapPost("/search", async (SearchRequest searchRequest, ISongService songService) =>
        {
            if (string.IsNullOrWhiteSpace(searchRequest.Query))
                return Results.BadRequest("Query must be provided.");

            var songs = await songService.SearchSongsAsync(searchRequest.Query, searchRequest.PageNum,
                searchRequest.Limit);

            return Results.Ok(songs);
        });

        groupSongs.MapGet("/recent", async (
            HttpContext context,
            ISongService songService,
            IJwtRequestReader jwtReader,
            [FromQuery] int limit = 50) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            var recentSongs = await songService.GetRecentlyPlayedSongsAsync(userId.Value, limit);
            return Results.Ok(recentSongs);
        });


        groupSongs.MapGet("/{id}", async (long id, ISongService songService) =>
        {
            var song = await songService.GetSongByIdAsync(id);
            return song is null ? Results.NotFound() : Results.Ok(song);
        }).WithName("GetSongById");


        groupSongs.MapGet("/emotion/{emotion}", async (string emotion, ISongService songService) =>
        {
            try
            {
                var songs = await songService.GetSongsByEmotionAsync(emotion);
                return Results.Ok(songs);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(ex.Errors);
            }
        });

        groupSongs.MapPost("/", async (CreateSongDto dto, ISongService songService) =>
        {
            try
            {
                var id = await songService.CreateSongAsync(dto);
                return Results.CreatedAtRoute("GetSongById", new {id}, new {Id = id});
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(ex.Errors);
            }
        });


        groupPlaylists.MapGet("/", async (
            HttpContext context,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, claims, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);
            var playlists = await playlistService.GetUserPlaylistsAsync(userId.Value);
            return Results.Ok(playlists);
        });

        groupPlaylists.MapGet("/favourite", async (
            HttpContext context,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);
            var playlists = await playlistService.GetUserPlaylistsByNameAsync(userId.Value, "Favourite");
            return Results.Ok(playlists.First().Songs);
        });

        groupPlaylists.MapPost("/create", async (
            HttpContext context,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader,
            CreatePlaylistDto createDto) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            try
            {
                var playlistId = await playlistService.CreatePlaylistAsync(createDto, userId.Value);

                return Results.Ok(new {Id = playlistId});
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });


        groupPlaylists.MapGet("/{playlistId}", async (
            HttpContext context,
            long playlistId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            var playlist = await playlistService.GetPlaylistByIdAsync(playlistId, userId.Value);
            return playlist != null ? Results.Ok(playlist) : Results.NotFound();
        });

        groupPlaylists.MapPost("/{playlistId}/add-song/{songId}", async (
            HttpContext context,
            long playlistId,
            long songId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            try
            {
                await playlistService.AddSongToPlaylistAsync(playlistId, songId, userId.Value);

                return Results.Ok(new {Message = "Song added to playlist successfully"});
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new {Error = ex.Message});
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        groupPlaylists.MapPost("/add-favourite-song/{songId}", async (
            HttpContext context,
            long songId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            try
            {
                await playlistService.AddSongToFavouritesPlaylistAsync(songId, userId.Value);

                return Results.Ok(new {Message = "Song added to playlist successfully"});
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new {Error = ex.Message});
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        groupPlaylists.MapDelete("/remove-favourite-song/{songId}", async (
            HttpContext context,
            long songId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await playlistService.RemoveSongFromFavouritesPlaylistAsync(songId, userId.Value);
            return Results.Ok(new {Message = "Song removed successfully"});
        });

        groupPlaylists.MapDelete("/{playlistId:long}/remove-song/{songId}", async (
            HttpContext context,
            long playlistId,
            long songId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await playlistService.RemoveSongFromPlaylistAsync(playlistId, songId, userId.Value);
            return Results.Ok(new {Message = "Song removed successfully"});
        });

        groupPlaylists.MapDelete("/{playlistId}", async (
            HttpContext context,
            long playlistId,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await playlistService.DeletePlaylistAsync(playlistId, userId.Value);
            return Results.Ok(new {Message = "Playlist deleted successfully"});
        });

        groupPlaylists.MapPatch("/{playlistId}", async (
            HttpContext context,
            long playlistId,
            UpdatePlaylistDto updateDto,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await playlistService.UpdatePlaylistInfoAsync(updateDto, playlistId, userId.Value);
            return Results.Ok(new {Message = "Playlist updated successfully"});
        });

        groupPlaylists.MapGet("/basic-info", async (
            HttpContext context,
            IPlaylistService playlistService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            var playlists = await playlistService.GetUserPlaylistsBasicInfoAsync(userId.Value);
            return Results.Ok(playlists);
        }).WithName("GetPlaylistsBasicInfo");


        groupHistory.MapPost("/add", async (
            HttpContext context,
            AddHistoryRecordDto dto,
            IHistoryRecordService historyService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await historyService.AddRecordAsync(userId.Value, dto.SongId, dto.Context);
            return Results.Ok(new {Message = "Record added"});
        });

        groupHistory.MapGet("/", async (
            HttpContext context,
            IHistoryRecordService historyService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            var history = await historyService.GetUserHistoryAsync(userId.Value);
            return Results.Ok(history);
        });

        groupHistory.MapDelete("/clear", async (
            HttpContext context,
            IHistoryRecordService historyService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            await historyService.ClearUserHistoryAsync(userId.Value);
            return Results.Ok(new {Message = "History cleared"});
        });

        groupHistory.MapGet("/last-songs", async (
            HttpContext context,
            IHistoryRecordService historyService,
            IJwtRequestReader jwtReader,
            [FromQuery] int limit = 50) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            if (limit <= 0 || limit > 100)
                return Results.BadRequest("Limit must be between 1 and 100");

            var songs = await historyService.GetLastUniqueSongsAsync(userId.Value, limit);
            return Results.Ok(songs);
        }).WithName("GetLastUniqueSongs");

        groupProfile.MapGet("/", async (
            HttpContext context,
            IProfileService profileService,
            IJwtRequestReader jwtReader) =>
        {
            var (success, userId, _, error) = await jwtReader.GetValidatedUserClaimsAsync(context);
            if (!success || userId == null)
                return string.IsNullOrEmpty(error) ? Results.Unauthorized() : Results.BadRequest(error);

            try
            {
                var profile = await profileService.GetUserProfileAsync(userId.Value);
                return Results.Ok(profile);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                context.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogError(ex, "Error getting user profile");
                return Results.Problem("Error getting user profile");
            }
        });
        groupDb.MapPost("/seed/{key}", async (
            string key,
            ApplicationDbContext dbContext,
            IWebHostEnvironment env,
            ILogger<Program> logger,
            IOptions<DbSeeds> dbSeeds) =>
        {
            if (dbSeeds.Value.SeedKey != key) return Results.BadRequest();
            if (await dbContext.Songs.AnyAsync())
            {
                logger.LogInformation("Seeding skipped - database already contains songs");
                return Results.Ok(new {Message = "Database already contains songs"});
            }

            var scriptPath = Path.Combine(env.ContentRootPath, "Scripts", "CreatingMusicDb.sql");

            if (!File.Exists(scriptPath))
            {
                logger.LogError("SQL script not found at {Path}", scriptPath);
                return Results.NotFound(new {Error = "SQL script not found"});
            }

            try
            {
                logger.LogInformation("Starting database seeding...");

                var sql = await File.ReadAllTextAsync(scriptPath);

                await dbContext.Database.ExecuteSqlRawAsync(sql);

                logger.LogInformation("Database seeded successfully");
                return Results.Ok(new {Message = "Database seeded successfully"});
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding database");
                return Results.Problem(
                    detail: "Error seeding database",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }).AllowAnonymous();
    }
}