namespace EmailService.Application.Settings;

public class EmailTemplates
{
    public EmailTemplateConfig EmailConfirmation { get; set; } = new();
    public EmailTemplateConfig PasswordReset { get; set; } = new();
    public EmailTemplateConfig EmailChangeConfirmation { get; set; } = new();
}