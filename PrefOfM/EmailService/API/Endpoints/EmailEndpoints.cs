using EmailService.API.Requests;
using EmailService.Services.Contracts;


namespace EmailService.API.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("")
            .WithTags("Email Service");

        group.MapPost("/send-confirmation", SendConfirmationEmail);
        group.MapPost("/reset-password", SendPasswordResetEmail);
        group.MapPost("/change-email", SendEmailChangeConfirmation);
    }

    private static async Task<IResult> SendConfirmationEmail(
        EmailConfirmationRequest request,
        IEmailSender emailSender,
        ILogger<Program> logger)
    {
        return await HandleEmailRequest(async () =>
        {
            await emailSender.SendEmailConfirmationAsync(request.Email, request.UserId, request.Token);
            logger.LogInformation("Confirmation email sent to {Email}", request.Email);
        }, 
        request.Email, 
        "Confirmation", 
        logger);
    }

    private static async Task<IResult> SendPasswordResetEmail(
        PasswordResetRequest request,
        IEmailSender emailSender,
        ILogger<Program> logger)
    {
        return await HandleEmailRequest(async () =>
        {
            await emailSender.SendPasswordResetAsync(request.Email, request.UserId, request.Token);
            logger.LogInformation("Password reset email sent to {Email}", request.Email);
        }, 
        request.Email, 
        "Password reset", 
        logger);
    }

    private static async Task<IResult> SendEmailChangeConfirmation(
        EmailChangeConfirmationRequest request,
        IEmailSender emailSender,
        ILogger<Program> logger)
    {
        return await HandleEmailRequest(async () =>
        {
            await emailSender.SendEmailChangeConfirmationAsync(
                request.NewEmail, 
                request.OldEmail, 
                request.UserId, 
                request.Token);
            logger.LogInformation("Email change confirmation sent to {Email}", request.NewEmail);
        }, 
        request.NewEmail, 
        "Email change confirmation", 
        logger);
    }

    private static async Task<IResult>  HandleEmailRequest(
        Func<Task> emailAction,
        string email,
        string emailType,
        ILogger logger)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Results.BadRequest(new { Message = $"{emailType} email is required" });
        }

        try
        {
            await emailAction();
            return Results.Ok(new { Message = $"{emailType} email sent successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending {EmailType} email to {Email}", emailType, email);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}