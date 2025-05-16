using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MusicService.Application.Features;

public static class JwkToPemConverter
{
    public static string ConvertJwkToPem(this string jwkJson)
    {
        var jsonDoc = JsonDocument.Parse(jwkJson);
        var keyElement = jsonDoc.RootElement.GetProperty("keys")[0];

        string n = keyElement.GetProperty("n").GetString()!;
        string e = keyElement.GetProperty("e").GetString()!;

        var rsaParams = new RSAParameters
        {
            Modulus = Base64UrlDecode(n),
            Exponent = Base64UrlDecode(e)
        };

        using var rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);

        byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        string publicKeyPem = PemEncode("PUBLIC KEY", publicKeyBytes);

        return publicKeyPem;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }

    private static string PemEncode(string label, byte[] bytes)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");

        string base64 = Convert.ToBase64String(bytes);
        for (int i = 0; i < base64.Length; i += 64)
        {
            builder.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));
        }

        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }
}