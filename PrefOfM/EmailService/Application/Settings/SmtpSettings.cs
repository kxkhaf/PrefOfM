namespace EmailService.Application.Settings;

public class SmtpSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 465;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public bool UseSsl { get; init; } = true;
    public int TimeoutMilliseconds { get; init; } = 10000;
    public string BaseUrl { get; init; } = string.Empty;
}