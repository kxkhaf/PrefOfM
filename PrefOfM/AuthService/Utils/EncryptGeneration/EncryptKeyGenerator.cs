using System.Security.Cryptography;

namespace AuthService.Utils.EncryptGeneration;

public class EncryptKeyGenerator
{
    public static (byte[], byte[]) GenerateKey()
    {
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);
        var hmacKey = new byte[32];
        RandomNumberGenerator.Fill(hmacKey);
        return (aesKey, hmacKey);
    }
}