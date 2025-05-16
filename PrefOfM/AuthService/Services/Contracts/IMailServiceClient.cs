namespace AuthService.Services.Contracts;

public interface IMailServiceClient
{
    Task SendEmailConfirmationAsync(string email, string userId, string token);
    Task SendPasswordResetAsync(string email, string userId, string token);
    Task SendEmailChangeConfirmationAsync(string newEmail, string oldEmail, string userId, string token);
}
