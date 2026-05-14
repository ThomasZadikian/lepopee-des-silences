using FluentAssertions;
using Microsoft.Extensions.Options;
using RPG_ESI07.Application.Configuration;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Services;

namespace RPG_ESI07.Tests.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            Secret = "SuperSecretKeyThatIsLongEnoughForHmacSha256!", // Minimum 32 caractères pour SHA256
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            MfaTokenExpirationMinutes = 5
        };

        var options = Options.Create(_settings);
        _service = new JwtTokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtFormat()
    {
        var user = new User { Id = 1, Username = "testuser" };

        var token = _service.GenerateAccessToken(user, "Player");

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // Header.Payload.Signature
    }

    [Fact]
    public void GenerateMfaToken_ReturnsValidJwtFormat()
    {
        var user = new User { Id = 1 };

        var token = _service.GenerateMfaToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void ValidateTokenAndGetUserId_ReturnsUserId_WhenTokenIsValid()
    {
        var user = new User { Id = 42, Username = "testuser" };
        var token = _service.GenerateAccessToken(user, "Player");

        var userId = _service.ValidateTokenAndGetUserId(token);

        userId.Should().Be(42);
    }

    [Fact]
    public void ValidateTokenAndGetUserId_ReturnsNull_WhenTokenIsInvalid()
    {
        var invalidToken = "invalid.token.format";

        var userId = _service.ValidateTokenAndGetUserId(invalidToken);

        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateTokenAndGetUserId_ReturnsNull_WhenTokenIsTampered()
    {
        var user = new User { Id = 1, Username = "testuser" };
        var token = _service.GenerateAccessToken(user, "Player");

        // Falsification de la signature
        var tamperedToken = token.Substring(0, token.Length - 5) + "abcde";

        var userId = _service.ValidateTokenAndGetUserId(tamperedToken);

        userId.Should().BeNull();
    }
}