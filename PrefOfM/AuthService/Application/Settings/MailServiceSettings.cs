namespace AuthService.Application.Settings;

public class MailServiceSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ConfirmEmailPath { get; init; } = string.Empty;
    public string PasswordResetPath { get; init; } = string.Empty;
    public string EmailChangeConfirmationPath { get; init; } = string.Empty;
    public int Timeout { get; init; } = 0;
}