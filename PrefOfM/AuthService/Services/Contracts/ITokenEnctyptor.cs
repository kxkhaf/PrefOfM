namespace AuthService.Services.Contracts;

/// <summary>
/// Предоставляет услуги шифрования и дешифрования данных токенов
/// </summary>
public interface ITokenEncryptor : IDisposable
{
    /// <summary>
    /// Шифрует указанный открытый текст
    /// </summary>
    /// <param name="plainText">Текст для шифрования</param>
    /// <returns>Зашифрованная строка в формате Base64</returns>
    /// <exception cref="ArgumentNullException">Если plainText равен null или пуст</exception>
    /// <exception cref="CryptographicException">Если шифрование не удалось</exception>
    string Encrypt(string plainText);

    /// <summary>  
    /// Дешифрует указанный зашифрованный текст  
    /// </summary>  
    /// <param name="cipherText">Зашифрованный текст в формате Base64</param>  
    /// <returns>Расшифрованная исходная строка</returns>  
    /// <exception cref="ArgumentNullException">Если cipherText равен null или пуст</exception>  
    /// <exception cref="CryptographicException">Если дешифрование или проверка не удались</exception>  
    string Decrypt(string cipherText);

    /// <summary>  
    /// Атомарно меняет ключи шифрования  
    /// </summary>  
    /// <param name="newAesKey">Новый ключ AES (в Base64, 32 байта)</param>  
    /// <param name="newHmacKey">Новый ключ HMAC (в Base64, 32 байта)</param>  
    /// <exception cref="ArgumentException">Если ключи имеют недопустимую длину</exception>  
    /// <exception cref="CryptographicException">Если смена ключей не удалась</exception>  
    void RotateKeys(string newAesKey, string newHmacKey);
}