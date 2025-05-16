using System.Net.Http.Headers;

namespace AuthService.Services.Implementations;

public static class BearerTokenGenerator
{
    public static string Generate(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

        return new AuthenticationHeaderValue("Bearer", accessToken).ToString();
    }
}