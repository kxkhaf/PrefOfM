namespace AuthService.Application.Settings;

public class PasswordSettings
{
    public int RequiredLength { get; init; } = 0;
    public bool RequireDigit { get; init; } = false;
    public bool RequireLowercase { get; init; } = false;
    public bool RequireUppercase { get; init; } = false;
    public bool RequireNonAlphanumeric { get; init; } = false;
    public int RequiredUniqueChars { get; init; } = 0;
}