namespace EmailService.Services.Contracts;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string body);
    Task SendEmailConfirmationAsync(string email, string userId, string token);
    Task SendPasswordResetAsync(string email, string userId, string token);
    Task SendEmailChangeConfirmationAsync(string newEmail, string oldEmail, string userId, string token);
}