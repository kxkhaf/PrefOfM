using System.Security.Claims;

namespace MusicService.Infrastructure.Identity;

public interface IJwtService
{
    /// <summary>
    /// Валидирует JWT токен и возвращает ClaimsPrincipal, если токен корректен.
    /// </summary>
    /// <param name="token">JWT токен</param>
    /// <returns>ClaimsPrincipal или null, если токен недействителен</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Читает данные из JWT БЕЗ проверки подписи (только для доверенных случаев!).
    /// </summary>
    ClaimsPrincipal ReadTokenWithoutValidation(string token);
}