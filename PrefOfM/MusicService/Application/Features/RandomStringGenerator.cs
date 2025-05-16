using System.Text;

namespace MusicService.Application.Features;

public static class RandomStringGenerator
{
    
    public static string Generate(int length = 250)
    {
        var chars = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        var random = new Random();
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            char c = chars[random.Next(chars.Length)];
            sb.Append(c);
        }
        return sb.ToString();
    }
}