using FluentAssertions;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

public sealed class SecurityTokenRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Leds.Player.Domain.Identity.EmailVerificationToken")]
    [InlineData("Leds.Player.Domain.Identity.PasswordResetToken")]
    public void SecurityToken_ShouldBeSingleUse(string typeName)
    {
        var token = CreateToken(typeName, TimeSpan.FromHours(1));

        FutureContract.InvokeInstance(token, "TryConsume", "token-hash", Now.AddMinutes(1))
            .Should().Be(true);
        FutureContract.InvokeInstance(token, "TryConsume", "token-hash", Now.AddMinutes(2))
            .Should().Be(false);
    }

    [Theory]
    [InlineData("Leds.Player.Domain.Identity.EmailVerificationToken")]
    [InlineData("Leds.Player.Domain.Identity.PasswordResetToken")]
    public void SecurityToken_ShouldRejectAnExpiredToken(string typeName)
    {
        var token = CreateToken(typeName, TimeSpan.FromMinutes(10));

        FutureContract.InvokeInstance(token, "TryConsume", "token-hash", Now.AddMinutes(10))
            .Should().Be(false);
    }

    [Theory]
    [InlineData("Leds.Player.Domain.Identity.EmailVerificationToken")]
    [InlineData("Leds.Player.Domain.Identity.PasswordResetToken")]
    public void WrongPresentedToken_ShouldNotConsumeTheRealToken(string typeName)
    {
        var token = CreateToken(typeName, TimeSpan.FromHours(1));

        FutureContract.InvokeInstance(token, "TryConsume", "wrong-hash", Now.AddMinutes(1))
            .Should().Be(false);
        FutureContract.InvokeInstance(token, "TryConsume", "token-hash", Now.AddMinutes(2))
            .Should().Be(true);
    }

    [Theory]
    [InlineData("Leds.Player.Domain.Identity.EmailVerificationToken")]
    [InlineData("Leds.Player.Domain.Identity.PasswordResetToken")]
    public void SecurityToken_ShouldPersistOnlyTheHash_NotTheRawToken(string typeName)
    {
        var tokenType = FutureContract.RequireDomainType(typeName);
        var token = CreateToken(typeName, TimeSpan.FromHours(1));

        FutureContract.Read<string>(token, "TokenHash").Should().Be("token-hash");
        tokenType.GetProperty("Token").Should().BeNull(
            "email-verification and password-reset tokens must never be persisted in reusable plaintext form");
    }

    private static object CreateToken(string typeName, TimeSpan lifetime)
    {
        var tokenType = FutureContract.RequireDomainType(typeName);
        return FutureContract.InvokeStatic(
            tokenType,
            "Issue",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "token-hash",
            Now,
            lifetime);
    }
}
