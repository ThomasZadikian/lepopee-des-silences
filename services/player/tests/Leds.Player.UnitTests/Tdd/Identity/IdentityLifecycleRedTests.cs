using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
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
        var recoveryCodes = RecoveryCodeSet.Create(["hash-a", "hash-b", "hash-c"]);

        recoveryCodes.TryConsume("hash-b").Should().BeTrue();
        recoveryCodes.TryConsume("hash-b").Should().BeFalse();
        recoveryCodes.RemainingCount.Should().Be(2);
    }

    [Fact]
    public void UnknownRecoveryCode_ShouldNotConsumeAnyValidCode()
    {
        var recoveryCodes = RecoveryCodeSet.Create(["hash-a", "hash-b"]);

        recoveryCodes.TryConsume("hash-unknown").Should().BeFalse();
        recoveryCodes.TryConsume("hash-a").Should().BeTrue();
        recoveryCodes.RemainingCount.Should().Be(1);
    }
}
