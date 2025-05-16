using AuthService.Application.Settings;
using AuthService.Services.Contracts;
using Microsoft.Extensions.Options;

namespace AuthService.Services.Implementations;

public class MailServiceClient(HttpClient httpClient, IOptions<MailServiceSettings> options) 
    : IMailServiceClient
{
    private readonly MailServiceSettings _settings = options.Value;

    public async Task SendEmailConfirmationAsync(string email, string userId, string token)
    {
        var request = new
        {
            Email = email,
            UserId = userId,
            Token = token,
            TemplateType = "email-confirmation"
        };

        await SendEmailAsync(_settings.ConfirmEmailPath, request);
    }

    public async Task SendPasswordResetAsync(string email, string userId, string token)
    {
        var request = new
        {
            Email = email,
            UserId = userId,
            Token = token,
            TemplateType = "password-reset"
        };

        await SendEmailAsync(_settings.PasswordResetPath, request);
    }

    private async Task SendEmailAsync(string path, object requestData)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}{path}", 
                requestData
            );

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Mail service error: {response.StatusCode} - {content}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Failed to connect to mail service", ex);
        }
    }
    
    public async Task SendEmailChangeConfirmationAsync(string newEmail, string oldEmail, string userId, string token)
    {
        var request = new
        {
            NewEmail = newEmail,
            OldEmail = oldEmail,
            UserId = userId,
            Token = token,
            TemplateType = "email-change-confirmation"
        };

        await SendEmailAsync(_settings.EmailChangeConfirmationPath, request);
    }
}