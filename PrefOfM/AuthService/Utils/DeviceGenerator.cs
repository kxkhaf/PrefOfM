namespace AuthService.Utils;

public static class DeviceIdGenerator
{
    public static string GenerateDeviceId()
    {
        return Guid.NewGuid().ToString();
    }
}