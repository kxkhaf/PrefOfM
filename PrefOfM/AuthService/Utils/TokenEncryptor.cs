using System.Security.Cryptography;
using AuthService.Application.Settings;
using AuthService.Services.Contracts;
using Microsoft.Extensions.Options;

namespace AuthService.Utils;

public class TokenEncryptor : ITokenEncryptor, IDisposable
{
    private byte[] _aesKey;
    private byte[] _hmacKey;
    private readonly Lock _keyLock = new();
    private readonly ILogger<TokenEncryptor> _logger;
    private bool _disposed;

    public TokenEncryptor(
        IOptions<EncryptionSettings> encryptionSettings,
        ILogger<TokenEncryptor> logger)
    {
        _logger = logger;
        var settings = encryptionSettings.Value;
        _aesKey = Convert.FromBase64String(settings.Key);
        _hmacKey = Convert.FromBase64String(settings.HmacKey);
        ValidateKeyLengths();
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));

        lock (_keyLock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TokenEncryptor));
            
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Padding = PaddingMode.PKCS7;

            byte[] encryptedData;
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                encryptedData = ms.ToArray();
            }

            var ivAndData = aes.IV.Concat(encryptedData).ToArray();
            var hmac = ComputeHmac(ivAndData);
            return Convert.ToBase64String(ivAndData.Concat(hmac).ToArray());
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentNullException(nameof(cipherText));

        lock (_keyLock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TokenEncryptor));
            
            var fullData = Convert.FromBase64String(cipherText);
            if (fullData.Length < 48)
                throw new CryptographicException("Invalid cipher text length");

            var ivAndData = fullData[..^32];
            var receivedHmac = fullData[^32..];

            var expectedHmac = ComputeHmac(ivAndData);
            if (!CryptographicOperations.FixedTimeEquals(receivedHmac, expectedHmac))
                throw new CryptographicException("HMAC validation failed");

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = ivAndData[..16];
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(ivAndData[16..]);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
    }

    public void RotateKeys(string newAesKey, string newHmacKey)
    {
        lock (_keyLock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TokenEncryptor));
            
            var newAesBytes = Convert.FromBase64String(newAesKey);
            var newHmacBytes = Convert.FromBase64String(newHmacKey);

            if (newAesBytes.Length != 32 || newHmacBytes.Length != 32)
                throw new ArgumentException("New keys must be 32 bytes long");

            var oldAes = Interlocked.Exchange(ref _aesKey, newAesBytes);
            var oldHmac = Interlocked.Exchange(ref _hmacKey, newHmacBytes);

            CryptographicOperations.ZeroMemory(oldAes);
            CryptographicOperations.ZeroMemory(oldHmac);
        }
    }

    private byte[] ComputeHmac(byte[] data)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        return hmac.ComputeHash(data);
    }

    private void ValidateKeyLengths()
    {
        if (_aesKey.Length != 32 || _hmacKey.Length != 32)
            throw new ArgumentException("Encryption keys must be exactly 32 bytes long");
    }

    public void Dispose()
    {
        lock (_keyLock)
        {
            if (_disposed) return;
            CryptographicOperations.ZeroMemory(_aesKey);
            CryptographicOperations.ZeroMemory(_hmacKey);
            _disposed = true;
        }
    }
}