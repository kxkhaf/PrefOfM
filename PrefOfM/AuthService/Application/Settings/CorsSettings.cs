namespace AuthService.Application.Settings;

public class CorsSettings
{
    public string PolicyName { get; init; } = string.Empty;
    public string[] AllowedOrigins { get; init; } = [];
    public string ExposedHeaders { get; init; } = string.Empty;
}