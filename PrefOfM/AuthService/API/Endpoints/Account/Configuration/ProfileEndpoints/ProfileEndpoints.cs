using System.Net;
using AuthService.API.Endpoints.Account.Requests;
using AuthService.API.Endpoints.Contracts;
using AuthService.Domain.Models;
using AuthService.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.API.Endpoints.Account.Configuration.ProfileEndpoints;

public class ProfileEndpoints : IEndpointGroup
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", GetProfile);
        group.MapPut("/email", UpdateEmail);
        group.MapPost("/email/confirm", ConfirmEmailChange);
        group.MapPut("/password", ChangePassword);
    }


    private async Task<IResult> GetProfile(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService)
    {
        var userData = await authTokenService.ValidateRefreshTokenAsync(httpContext);
        if (userData == null)
            return Results.Unauthorized();

        var (userId, _) = userData.Value;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Results.NotFound("User not found");

        return Results.Ok(new
        {
            id = user.Id,
            username = user.UserName,
            email = user.Email,
            birthday = user.Birthday
        });
    }

    private async Task<IResult> UpdateEmail(
        UpdateEmailRequest request,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService,
        IMailServiceClient mailClient,
        ILogger<Program> logger)
    {
        var userData = await authTokenService.ValidateRefreshTokenAsync(httpContext);
        if (userData == null) return Results.Unauthorized();

        var (userId, _) = userData.Value;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Results.NotFound("User not found");
        
        if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            return Results.BadRequest("Current password is required");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordValid)
        {
            return Results.BadRequest("Invalid current password");
        }

        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new {message = "Email is the same as current", email = user.Email});
        }

        var existingUser = await userManager.FindByEmailAsync(request.NewEmail);
        if (existingUser != null)
        {
            return Results.Conflict("Email is already in use");
        }

        try
        {
            var token = await userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
            var encodedToken = WebUtility.UrlEncode(token);

            await mailClient.SendEmailChangeConfirmationAsync(
                newEmail: request.NewEmail,
                oldEmail: user.Email!,
                userId: userId,
                token: encodedToken);

            logger.LogInformation("Email change confirmation sent to {Email}", request.NewEmail);
            return Results.Ok(new
            {
                message = "Confirmation email sent",
                userId = userId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email change initiation failed");
            return Results.Problem("Failed to initiate email change");
        }
    }

    private async Task<IResult> ConfirmEmailChange(
        ConfirmEmailChangeRequest request,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService,
        ILogger<Program> logger)
    {
        var userData = await authTokenService.ValidateRefreshTokenAsync(httpContext);
        if (userData == null) return Results.Unauthorized();

        var (userId, _) = userData.Value;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Results.NotFound("User not found");

        try
        {
            var fixedToken = request.Token.Replace(" ", "+");

            var decodedToken = Uri.UnescapeDataString(fixedToken);

            if (decodedToken.Contains('%'))
            {
                decodedToken = Uri.UnescapeDataString(decodedToken);
            }

            var result = await userManager.ChangeEmailAsync(user, request.NewEmail, decodedToken);

            if (!result.Succeeded && user.Email != request.NewEmail)
            {
                logger.LogWarning("Email change failed: {Errors}", result.Errors);
                return Results.BadRequest(new {errors = result.Errors});
            }

            await authTokenService.SetAuthTokenAsync(httpContext, userId);

            logger.LogInformation("Email changed for user {UserId}", userId);
            return Results.Ok(new
            {
                message = "Email changed successfully",
                email = request.NewEmail
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email change confirmation failed");
            return Results.Problem("Email change confirmation failed");
        }
    }


    private async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService,
        ILogger<Program> logger)
    {
        var userData = await authTokenService.ValidateRefreshTokenAsync(httpContext);
        if (userData == null)
            throw new SecurityTokenException("Invalid refresh token");
        var (userId, _) = userData.Value;
        if (userId != request.UserId)
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User not found");
            return Results.NotFound("User not found");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordValid)
        {
            logger.LogWarning("Invalid current password for user {UserId}", user.Id);
            return Results.BadRequest("Current password is incorrect");
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword
        );

        if (!result.Succeeded)
        {
            logger.LogWarning("Password change failed for user {UserId}: {Errors}",
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(new
            {
                message = "Password change failed",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        logger.LogInformation("Password changed successfully for user {UserId}", user.Id);
        return Results.Ok(new {message = "Password has been changed successfully"});
    }
}