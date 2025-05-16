using System.Security.Cryptography;
using AuthService.API.Endpoints.Contracts;
using AuthService.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.API.Endpoints.Account.Configuration.WellKnownEndpoints;

public class WellKnownEndpoints : IEndpointGroup
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/.well-known/jwks.json", GetJwks)
            .WithTags("Discovery")
            .ExcludeFromDescription();
    }
    private async Task<IResult> GetJwks(IOptions<JwtSettings> options)
    {
        var settings = options.Value;
        var rsa = RSA.Create();

        rsa.ImportFromPem(await File.ReadAllTextAsync(settings.RsaPublicKeyPath));

        return Results.Json(new
        {
            keys = new[]
            {
                new
                {
                    kty = settings.KeyType,
                    use = settings.PublicKeyUse,
                    kid = settings.KeyId,
                    alg = settings.Algorithm,
                    n = Base64UrlEncoder.Encode(rsa.ExportParameters(false).Modulus!),
                    e = Base64UrlEncoder.Encode(rsa.ExportParameters(false).Exponent!)
                }
            }
        });
    }
}