using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

/// <summary>
/// First RED slice for SFD-010 v1.1: account registration foundation.
/// These tests intentionally describe the target contract before the production
/// implementation exists. Keep this suite deterministic: no network, database,
/// wall-clock access or random values are allowed here.
/// </summary>
public sealed class AccountRegistrationRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewAccount_ShouldStartWithoutCharacter_UntilArchetypeSelection()
    {
        var account = PlayerProfile.Create("Nocturne", Now);

        account.Roster.Characters.Should().BeEmpty(
            "character creation belongs to the archetype-selection onboarding step");
    }

    [Fact]
    public void EmailAddress_ShouldNormalizeTrimAndCase()
    {
        var emailType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");
        var email = FutureContract.InvokeStatic(emailType, "Create", "  Player@Example.COM  ");

        FutureContract.Read<string>(email, "Value")
            .Should().Be("player@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("@missing-local.example")]
    public void EmailAddress_ShouldRejectInvalidValues(string value)
    {
        var emailType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");

        var act = () => FutureContract.InvokeStatic(emailType, "Create", value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PasswordPolicy_ShouldRejectPasswordsShorterThanTwelveCharacters()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.PasswordPolicy");

        var act = () => FutureContract.InvokeStatic(policyType, "EnsureAcceptable", "elevenchars");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PasswordPolicy_ShouldNotRequireArtificialCharacterClasses()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.PasswordPolicy");

        var act = () => FutureContract.InvokeStatic(policyType, "EnsureAcceptable", "correcthorse");

        act.Should().NotThrow(
            "the approved policy requires 12+ characters but no forced uppercase/digit/symbol composition");
    }

    [Fact]
    public void RegisteredIdentity_ShouldStartWithEmailUnverifiedAndMfaNotConfigured()
    {
        var identity = CreateRegisteredIdentity();

        FutureContract.Read<bool>(identity, "IsEmailVerified").Should().BeFalse();
        FutureContract.Read<bool>(identity, "IsMfaConfigured").Should().BeFalse();
    }

    [Fact]
    public void RegisteredIdentity_ShouldDefaultToPlayerRole()
    {
        var identity = CreateRegisteredIdentity();

        FutureContract.Read<object>(identity, "Role")
            .ToString().Should().Be("Player");
    }

    internal static object CreateRegisteredIdentity()
    {
        var emailType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");
        var email = FutureContract.InvokeStatic(emailType, "Create", "player@example.com");
        var identityType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.UserIdentity");

        return FutureContract.InvokeStatic(
            identityType,
            "Register",
            email,
            "argon2id$unit-test-hash",
            Now);
    }
}
