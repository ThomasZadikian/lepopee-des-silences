using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Identity;
using Microsoft.Extensions.Configuration;

namespace Leds.Player.Infrastructure.Security;

public sealed class HmacAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public HmacAccessTokenIssuer(IConfiguration configuration)
    {
        var signingKey = configuration["Authentication:Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes.");

        _signingKey = Encoding.UTF8.GetBytes(signingKey);
        _issuer = configuration["Authentication:Jwt:Issuer"] ?? "leds-player";
        _audience = configuration["Authentication:Jwt:Audience"] ?? "leds-game-client";
    }

    public AccessTokenResult Issue(
        UserIdentity identity,
        Guid sessionId,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        var expires = now.Add(lifetime);
        var header = JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" });
        var payload = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["sub"] = identity.AccountId.ToString("D"),
            ["identity_id"] = identity.Id.ToString("D"),
            ["sid"] = sessionId.ToString("D"),
            ["role"] = identity.Role.ToString(),
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds()
        });

        var unsigned = $"{Base64Url(header)}.{Base64Url(payload)}";
        using var hmac = new HMACSHA256(_signingKey);
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(unsigned));
        return new AccessTokenResult($"{unsigned}.{Base64Url(signature)}", expires);
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
