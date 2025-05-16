namespace EmailService.Application.Settings;

public class EmailTemplateConfig
{
    public string Subject { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
    public string SenderName { get; set; } = "Служба поддержки";
}