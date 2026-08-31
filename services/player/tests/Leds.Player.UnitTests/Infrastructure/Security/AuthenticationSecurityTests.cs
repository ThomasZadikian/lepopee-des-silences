using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.Player.UnitTests.Infrastructure.Security;

public sealed class AuthenticationSecurityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 8, 0, 0, TimeSpan.Zero);
    private const string SigningKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void Constructor_ShouldRequireMfaProtectionKey()
    {
        var act = () => new AuthenticationSecurity(Config());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_ShouldRejectMalformedOrWrongSizedProtectionKeys()
    {
        var malformed = () => new AuthenticationSecurity(Config(("Authentication:MfaProtectionKey", "not-base64")));
        var tooShort = () => new AuthenticationSecurity(Config(("Authentication:MfaProtectionKey", Convert.ToBase64String(new byte[16]))));

        malformed.Should().Throw<InvalidOperationException>();
        tooShort.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PasswordHash_ShouldUseArgon2idAndVerifyOnlyCorrectPassword()
    {
        var sut = Security();

        var hash = sut.HashPassword("correcthorse");

        hash.Should().StartWith("argon2id$v=19$m=65536,t=3,p=2$");
        sut.VerifyPassword("correcthorse", hash).Should().BeTrue();
        sut.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("password", "")]
    [InlineData("password", "not-an-argon-hash")]
    [InlineData("password", "argon2id$v=19$m=nope,t=3,p=2$c2FsdA==$aGFzaA==")]
    [InlineData("password", "argon2id$v=19$m=65536,t=3,p=2$%%%$aGFzaA==")]
    [InlineData("password", "argon2id$v=19$m=65536,t=3,p=2$c2FsdA==$%%%")]
    public void VerifyPassword_ShouldReturnFalseForInvalidMaterial(string password, string hash)
    {
        Security().VerifyPassword(password, hash).Should().BeFalse();
    }

    [Fact]
    public void OpaqueToken_ShouldBeRandomAndPersistableOnlyThroughHash()
    {
        var sut = Security();

        var first = sut.GenerateOpaqueToken();
        var second = sut.GenerateOpaqueToken();

        first.Value.Should().NotBeNullOrWhiteSpace().And.NotBe(second.Value);
        first.Hash.Should().Be(sut.HashOpaqueToken(first.Value));
        first.Hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashOpaqueToken_ShouldRejectBlankValues(string value)
    {
        var act = () => Security().HashOpaqueToken(value);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MfaEnrollment_ShouldProduceGoogleAuthenticatorCompatibleTotpMaterial()
    {
        var sut = Security(("Authentication:TotpIssuer", "Palais Test"));

        var enrollment = sut.CreateMfaEnrollment(EmailAddress.Create("player@example.com"));

        enrollment.ManualEntryKey.Should().MatchRegex("^[A-Z2-7]+$");
        enrollment.ProtectedSecret.Should().NotBe(enrollment.ManualEntryKey);
        enrollment.OtpAuthUri.Should().StartWith("otpauth://totp/Palais%20Test:player%40example.com");
        enrollment.OtpAuthUri.Should().Contain($"secret={enrollment.ManualEntryKey}");
        enrollment.OtpAuthUri.Should().Contain("algorithm=SHA1&digits=6&period=30");
    }

    [Fact]
    public void MfaEnrollment_ShouldUseDefaultIssuerWhenNoneConfigured()
    {
        var enrollment = Security().CreateMfaEnrollment(EmailAddress.Create("player@example.com"));
        enrollment.OtpAuthUri.Should().Contain("issuer=L%27%C3%A9pop%C3%A9e%20des%20silences");
    }

    [Fact]
    public void Totp_ShouldAcceptCurrentAndAdjacentWindowCodes()
    {
        var sut = Security();
        var enrollment = sut.CreateMfaEnrollment(EmailAddress.Create("player@example.com"));
        var secret = DecodeBase32(enrollment.ManualEntryKey);

        foreach (var offset in new[] { -30, 0, 30 })
        {
            var instant = Now.AddSeconds(offset);
            var code = ComputeTotp(secret, instant);
            sut.VerifyTotp(enrollment.ProtectedSecret, code, Now).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void Totp_ShouldRejectMalformedCodes(string code)
    {
        Security().VerifyTotp("anything", code, Now).Should().BeFalse();
    }

    [Fact]
    public void Totp_ShouldRejectCorruptedProtectedSecretAndWrongCode()
    {
        var sut = Security();
        var enrollment = sut.CreateMfaEnrollment(EmailAddress.Create("player@example.com"));

        sut.VerifyTotp("not-base64", "123456", Now).Should().BeFalse();
        sut.VerifyTotp(Convert.ToBase64String(new byte[10]), "123456", Now).Should().BeFalse();
        sut.VerifyTotp(enrollment.ProtectedSecret, "000000", Now).Should().BeFalse();
    }

    [Fact]
    public void AccessTokenIssuer_ShouldRequireStrongSigningKey()
    {
        var missing = () => new HmacAccessTokenIssuer(Config());
        var shortKey = () => new HmacAccessTokenIssuer(Config(("Authentication:Jwt:SigningKey", "too-short")));

        missing.Should().Throw<InvalidOperationException>();
        shortKey.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AccessTokenIssuer_ShouldEmitSignedJwtWithExpectedClaimsAndExpiry()
    {
        var issuer = new HmacAccessTokenIssuer(Config(
            ("Authentication:Jwt:SigningKey", SigningKey),
            ("Authentication:Jwt:Issuer", "test-issuer"),
            ("Authentication:Jwt:Audience", "test-audience")));
        var identity = Identity();
        var sessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var result = issuer.Issue(identity, sessionId, Now, TimeSpan.FromMinutes(15));

        result.ExpiresAtUtc.Should().Be(Now.AddMinutes(15));
        var parts = result.Token.Split('.');
        parts.Should().HaveCount(3);
        var payload = JsonDocument.Parse(Base64UrlDecode(parts[1])).RootElement;
        payload.GetProperty("sub").GetString().Should().Be(identity.AccountId.ToString("D"));
        payload.GetProperty("identity_id").GetString().Should().Be(identity.Id.ToString("D"));
        payload.GetProperty("sid").GetString().Should().Be(sessionId.ToString("D"));
        payload.GetProperty("role").GetString().Should().Be("Player");
        payload.GetProperty("iss").GetString().Should().Be("test-issuer");
        payload.GetProperty("aud").GetString().Should().Be("test-audience");
        VerifyJwtSignature(parts).Should().BeTrue();
    }

    [Fact]
    public void AccessTokenIssuer_ShouldUseDefaultIssuerAndAudience()
    {
        var issuer = new HmacAccessTokenIssuer(Config(("Authentication:Jwt:SigningKey", SigningKey)));
        var token = issuer.Issue(Identity(), Guid.NewGuid(), Now, TimeSpan.FromMinutes(1)).Token;
        var payload = JsonDocument.Parse(Base64UrlDecode(token.Split('.')[1])).RootElement;

        payload.GetProperty("iss").GetString().Should().Be("leds-player");
        payload.GetProperty("aud").GetString().Should().Be("leds-game-client");
    }

    [Fact]
    public void AccessTokenIssuer_ShouldRejectNonPositiveLifetime()
    {
        var issuer = new HmacAccessTokenIssuer(Config(("Authentication:Jwt:SigningKey", SigningKey)));
        var act = () => issuer.Issue(Identity(), Guid.NewGuid(), Now, TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task DevelopmentEmailSender_ShouldLogVerificationAndResetLinksWithoutSmtp()
    {
        var logger = new Mock<ILogger<SmtpAccountEmailSender>>();
        var sender = new SmtpAccountEmailSender(
            Config(
                ("Authentication:Email:Mode", "Log"),
                ("Authentication:Email:PublicClientBaseUrl", "https://game.example/")),
            logger.Object);
        var recipient = EmailAddress.Create("player@example.com");

        await sender.SendVerificationEmailAsync(recipient, "verify token", CancellationToken.None);
        await sender.SendPasswordResetEmailAsync(recipient, "reset token", CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SmtpEmailSender_ShouldRequireHostAndFromOutsideLogMode()
    {
        var sender = new SmtpAccountEmailSender(Config(), Mock.Of<ILogger<SmtpAccountEmailSender>>());
        var act = () => sender.SendVerificationEmailAsync(
            EmailAddress.Create("player@example.com"),
            "token",
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static AuthenticationSecurity Security(params (string Key, string Value)[] extras)
    {
        var values = new List<(string Key, string Value)>
        {
            ("Authentication:MfaProtectionKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()))
        };
        values.AddRange(extras);
        return new AuthenticationSecurity(Config(values.ToArray()));
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();

    private static UserIdentity Identity() => UserIdentity.RegisterForAccount(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        EmailAddress.Create("player@example.com"),
        "hash",
        Now);

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in value)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits < 8)
                continue;
            output.Add((byte)((buffer >> (bits - 8)) & 0xff));
            bits -= 8;
        }
        return output.ToArray();
    }

    private static string ComputeTotp(byte[] secret, DateTimeOffset instant)
    {
        var counter = instant.ToUnixTimeSeconds() / 30;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private static bool VerifyJwtSignature(string[] parts)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningKey));
        var expected = hmac.ComputeHash(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
        return CryptographicOperations.FixedTimeEquals(expected, Base64UrlDecode(parts[2]));
    }
}
