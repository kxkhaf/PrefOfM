using System.Net;
using AuthService.API.Endpoints.Account.Requests;
using AuthService.API.Endpoints.Contracts;
using AuthService.Domain.Models;
using AuthService.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.API.Endpoints.Account.Configuration.AuthEndpoints;

public class AuthEndpoints : IEndpointGroup
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("/sign-in", SignIn);
        app.MapPost("/sign-up", SignUp);
        app.MapPost("/confirm-email", ConfirmEmail);
        app.MapPost("/resend-confirmation", ResendConfirmation);
        app.MapPost("/forgot-password", ForgotPassword);
        app.MapPost("/reset-password", ResetPassword);
        app.MapPost("/verify-reset-token", VerifyResetToken);
        app.MapGet("/refresh", RefreshToken);
        app.MapPost("/logout", Logout).RequireAuthorization();
        app.MapPost("/logout/all", LogoutAll).RequireAuthorization();
    }


    private async Task<IResult> SignIn(
        LoginRequest model,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuthTokenService authTokenService,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(model.Login) || string.IsNullOrWhiteSpace(model.Password))
        {
            return Results.BadRequest(new {message = "Login/Email and password are required."});
        }

        var user = model.Login.Contains('@')
            ? await userManager.FindByEmailAsync(model.Login)
            : await userManager.FindByNameAsync(model.Login);

        if (user == null)
        {
            logger.LogWarning("Login attempt for non-existent user: {Login}", model.Login);
            return Results.Unauthorized();
        }

        if (!user.EmailConfirmed)
        {
            logger.LogWarning("Login attempt for unconfirmed email: {Email}", user.Email);
            return Results.Json(
                new
                {
                    message = "Email not confirmed. Please check your email.",
                    userId = user.Id,
                    email = user.Email
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {UserId} is locked out", user.Id);
            return Results.Json(
                new {message = "Account is temporarily locked. Try again later."},
                statusCode: StatusCodes.Status423Locked);
        }

        if (!result.Succeeded)
        {
            logger.LogWarning("Invalid password attempt for user: {UserId}", user.Id);
            return Results.Unauthorized();
        }

        try
        {
            await authTokenService.SetAuthTokenAsync(httpContext, user.Id.ToString());

            logger.LogInformation("User {UserId} successfully signed in", user.Id);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during sign-in process for user: {UserId}", user.Id);
            return Results.Problem("Internal server error during authentication");
        }
    }


    private async Task<IResult> SignUp(
        RegisterRequest model,
        UserManager<ApplicationUser> userManager,
        IMailServiceClient mailClient,
        ILogger<Program> logger)
    {
        var passwordValidator = new PasswordValidator<ApplicationUser>();
        var validationResult = await passwordValidator.ValidateAsync(userManager, null!, model.Password);

        if (!validationResult.Succeeded)
        {
            return Results.BadRequest(new
            {
                message = "Password does not meet requirements",
                errors = validationResult.Errors.Select(e => e.Description)
            });
        }

        var existingUser = await userManager.FindByNameAsync(model.UserName);
        if (existingUser is not null)
            return Results.Conflict(new {message = "Username is already taken."});

        var existingEmail = await userManager.FindByEmailAsync(model.Email);
        if (existingEmail is not null)
            return Results.Conflict(new {message = "Email is already in use."});

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.UserName,
            Email = model.Email,
            Birthday = model.Birthday,
            EmailConfirmed = false
        };

        var creationResult = await userManager.CreateAsync(user, model.Password);
        if (!creationResult.Succeeded)
        {
            logger.LogWarning("User creation failed: {Errors}", creationResult.Errors);
            return Results.BadRequest(new
            {
                message = "Registration failed",
                errors = creationResult.Errors.Select(e => e.Description)
            });
        }

        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(emailToken);

        try
        {
            await mailClient.SendEmailConfirmationAsync(user.Email, user.Id.ToString(), encodedToken);
            logger.LogInformation("Confirmation email sent to {Email}", user.Email);

            return Results.Ok(new
            {
                message = "Registration successful. Please check your email to confirm your account.",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email");
            await userManager.DeleteAsync(user);

            return Results.Problem(
                detail: "Failed to send confirmation email. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }


    private async Task<IResult> ConfirmEmail(
        ConfirmEmailRequest model,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService)
    {
        if (string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.Token))
            return Results.BadRequest(new {message = "User ID and token are required."});

        if (!Guid.TryParse(model.UserId, out var userGuid))
            return Results.BadRequest(new {message = "Invalid user ID format."});

        var user = await userManager.FindByIdAsync(userGuid.ToString());
        if (user is null)
            return Results.NotFound(new {message = "User not found."});

        if (user.EmailConfirmed)
            return Results.Ok(new {message = "Email already confirmed."});

        var decodedToken = WebUtility.UrlDecode(model.Token);
        var confirmationResult = await userManager.ConfirmEmailAsync(user, decodedToken);

        if (!confirmationResult.Succeeded)
            return Results.BadRequest(new
            {
                message = "Email confirmation failed.",
                errors = confirmationResult.Errors.Select(e => e.Description)
            });

        await authTokenService.SetAuthTokenAsync(httpContext, user.Id.ToString());

        return Results.Ok(new
        {
            message = "Email successfully confirmed."
        });
    }

    private async Task<IResult> ResendConfirmation(
        ResendConfirmationRequest model,
        UserManager<ApplicationUser> userManager,
        IMailServiceClient mailClient,
        ILogger<Program> logger)

    {
        var user = await userManager.FindByNameAsync(model.UserName);
        if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
        {
            return Results.BadRequest(new {message = "Invalid username or password."});
        }

        if (user.EmailConfirmed)
            return Results.Ok(new {message = "Email is already confirmed."});

        if (model.Email != user.Email)
        {
            var existingUserWithEmail = await userManager.FindByEmailAsync(model.Email);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
            {
                return Results.Conflict(new {message = "This email is already registered."});
            }

            user.Email = model.Email;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogWarning("Failed to update user email: {Errors}", updateResult.Errors);
                return Results.BadRequest(new
                {
                    message = "Failed to update email address.",
                    errors = updateResult.Errors.Select(e => e.Description)
                });
            }
        }

        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(emailToken);

        try
        {
            await mailClient.SendEmailConfirmationAsync(model.Email, user.Id.ToString(), encodedToken);
            logger.LogInformation("Confirmation email sent to {Email} (requested by user {UserId})",
                model.Email, user.Id);

            return Results.Ok(new
            {
                message = $"Confirmation email sent to {model.Email}",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", model.Email);
            return Results.Problem(
                detail: "Failed to send confirmation email. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

private async Task<IResult> ForgotPassword(
    ForgotPasswordRequest request,
    UserManager<ApplicationUser> userManager,
    IMailServiceClient mailClient,
    ILogger<Program> logger)

{
    var user = await userManager.FindByEmailAsync(request.Email);

    if (user == null || user.UserName != request.UserName)
    {
        return Results.Ok(new
        {
            message = "If the account exists, a reset link has been sent.",
            userId = user?.Id.ToString()
        });
    }

    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var encodedToken = WebUtility.UrlEncode(token);

    try
    {
        await mailClient.SendPasswordResetAsync(
            user.Email!,
            user.Id.ToString(),
            encodedToken
        );

        logger.LogInformation("Password reset email sent to {Email}", user.Email);
        return Results.Ok(new
        {
            message = "Reset link sent if account exists",
            userId = user.Id
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send password reset email");
        return Results.Problem("Failed to send reset email. Please try again later.");
    }
}

private async Task<IResult> ResetPassword(
    ResetPasswordRequest request,
    UserManager<ApplicationUser> userManager)

{
    if (!Guid.TryParse(request.UserId, out var userId))
    {
        return Results.BadRequest("Invalid user ID");
    }

    var user = await userManager.FindByIdAsync(request.UserId);
    if (user == null)
    {
        return Results.BadRequest("Invalid reset token");
    }

    var decodedToken = WebUtility.UrlDecode(request.Token);
    var result = await userManager.ResetPasswordAsync(
        user,
        decodedToken,
        request.NewPassword
    );

    if (!result.Succeeded)
    {
        return Results.BadRequest(new
        {
            message = "Password reset failed",
            errors = result.Errors.Select(e => e.Description)
        });
    }

    return Results.Ok(new {message = "Password has been reset successfully"});
}

private async Task<IResult> VerifyResetToken(
    VerifyResetTokenRequest request,
    UserManager<ApplicationUser> userManager)

{
    var user = await userManager.FindByIdAsync(request.UserId);
    if (user == null) return Results.BadRequest(new {valid = false});

    try
    {
        var decodedToken = WebUtility.UrlDecode(request.Token);
        return Results.Ok(new
        {
            valid = await userManager.VerifyUserTokenAsync(user,
                userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", decodedToken)
        });
    }
    catch
    {
        return Results.BadRequest(new {valid = false});
    }
}

private async Task<IResult> RefreshToken(
    HttpContext httpContext, IAuthTokenService authTokenService)

{
    if (!httpContext.Request.Cookies.TryGetValue("refresh_token", out var encryptedRefreshToken))
    {
        return Results.Unauthorized();
    }

    try
    {
        await authTokenService.SetAuthTokenAsync(httpContext);
        return Results.Ok();
    }
    catch (SecurityTokenException ex)
    {
        return Results.Unauthorized();
    }
}

private async Task<IResult> Logout(
    HttpContext httpContext, IJwtService jwtService, ICookieService cookieService)
{
    var refreshToken = cookieService.GetRefreshToken(httpContext);
    await jwtService.LogoutDeviceAsync(refreshToken ?? "");
    httpContext.Response.Cookies.Delete("refresh_token");

    return Results.Ok();
}

private async Task<IResult> LogoutAll(
    HttpContext httpContext, IJwtService jwtService, ICookieService cookieService)
{
    var refreshToken = cookieService.GetRefreshToken(httpContext);
    await jwtService.LogoutAllDevicesAsync(refreshToken ?? "");
    httpContext.Response.Cookies.Delete("refresh_token");

    return Results.Ok();
}

}