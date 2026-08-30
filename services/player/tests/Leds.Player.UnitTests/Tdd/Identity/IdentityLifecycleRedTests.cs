using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

public sealed class IdentityLifecycleRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VerifyEmail_ShouldMarkIdentityAsVerified()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();

        FutureContract.InvokeInstance(identity, "VerifyEmail", Now);

        FutureContract.Read<bool>(identity, "IsEmailVerified").Should().BeTrue();
    }

    [Fact]
    public void ConfigureMfa_ShouldBeRejectedBeforeEmailVerification()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();

        var act = () => FutureContract.InvokeInstance(
            identity,
            "ConfigureMfa",
            "protected-totp-secret",
            Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ConfigureMfa_ShouldSucceedAfterEmailVerification()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();
        FutureContract.InvokeInstance(identity, "VerifyEmail", Now);

        FutureContract.InvokeInstance(
            identity,
            "ConfigureMfa",
            "protected-totp-secret",
            Now.AddMinutes(1));

        FutureContract.Read<bool>(identity, "IsMfaConfigured").Should().BeTrue();
    }

    [Fact]
    public void ChangeEmail_ShouldRequireVerificationOfTheNewAddress()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();
        FutureContract.InvokeInstance(identity, "VerifyEmail", Now);
        FutureContract.InvokeInstance(
            identity,
            "ConfigureMfa",
            "protected-totp-secret",
            Now.AddMinutes(1));

        var emailType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");
        var newEmail = FutureContract.InvokeStatic(emailType, "Create", "new-address@example.com");
        FutureContract.InvokeInstance(identity, "ChangeEmail", newEmail, Now.AddMinutes(2));

        FutureContract.Read<bool>(identity, "IsEmailVerified").Should().BeFalse();
        FutureContract.Read<bool>(identity, "IsMfaConfigured").Should().BeTrue(
            "changing an email address must not silently disable the mandatory second factor");
    }

    [Fact]
    public void RecoveryCode_ShouldBeConsumableOnlyOnce()
    {
        var recoveryType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.RecoveryCodeSet");
        var recoveryCodes = FutureContract.InvokeStatic(
            recoveryType,
            "Create",
            new[] { "hash-a", "hash-b", "hash-c" });

        var firstUse = FutureContract.InvokeInstance(recoveryCodes, "TryConsume", "hash-b");
        var secondUse = FutureContract.InvokeInstance(recoveryCodes, "TryConsume", "hash-b");

        firstUse.Should().Be(true);
        secondUse.Should().Be(false);
    }

    [Fact]
    public void UnknownRecoveryCode_ShouldNotConsumeAnyValidCode()
    {
        var recoveryType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.RecoveryCodeSet");
        var recoveryCodes = FutureContract.InvokeStatic(
            recoveryType,
            "Create",
            new[] { "hash-a", "hash-b" });

        var unknown = FutureContract.InvokeInstance(recoveryCodes, "TryConsume", "hash-unknown");
        var valid = FutureContract.InvokeInstance(recoveryCodes, "TryConsume", "hash-a");

        unknown.Should().Be(false);
        valid.Should().Be(true);
    }
}
