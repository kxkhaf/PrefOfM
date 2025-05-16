namespace AuthService.Application.Settings;

public class EncryptionSettings
{
    public string Key { get; init; } = null!;
    public string HmacKey { get; init; } = null!;
}