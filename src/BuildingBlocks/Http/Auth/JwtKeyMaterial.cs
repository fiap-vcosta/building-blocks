using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Http.Auth;

internal static class JwtKeyMaterial
{
    public static RsaSecurityKey CreateRsaSecurityKey(string publicKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);

        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return new RsaSecurityKey(rsa);
    }

    public static void ValidateScheme(string schemeName, JwtSchemeOptions scheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);
        ArgumentNullException.ThrowIfNull(scheme);

        if (string.IsNullOrWhiteSpace(scheme.Issuer))
        {
            throw new InvalidOperationException($"Jwt:{schemeName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(scheme.Audience))
        {
            throw new InvalidOperationException($"Jwt:{schemeName}:Audience is required.");
        }

        var hasPem = !string.IsNullOrWhiteSpace(scheme.PublicKeyPem);
        var hasJwks = !string.IsNullOrWhiteSpace(scheme.JwksUrl);
        if (hasPem == hasJwks)
        {
            throw new InvalidOperationException(
                $"Jwt:{schemeName} requires exactly one of PublicKeyPem or JwksUrl.");
        }
    }
}
