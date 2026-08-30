using FluentAssertions;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

public sealed class AuthenticationPolicyRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InteractiveAuthentication_ShouldRequireMfa()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AuthenticationPolicy");

        FutureContract.InvokeStatic(policyType, "RequiresMfa", "Interactive")
            .Should().Be(true);
    }

    [Fact]
    public void SilentRefresh_ShouldNotRequireMfa()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AuthenticationPolicy");

        FutureContract.InvokeStatic(policyType, "RequiresMfa", "Refresh")
            .Should().Be(false,
                "a valid refresh session must renew silently and never interrupt an active Run with an MFA prompt");
    }

    [Fact]
    public void UnverifiedEmail_ShouldBlockInteractiveSessionCreation()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AuthenticationPolicy");

        FutureContract.InvokeStatic(policyType, "CanStartInteractiveSession", identity)
            .Should().Be(false);
    }

    [Fact]
    public void VerifiedEmailWithoutMfa_ShouldStillBlockInteractiveSessionCreation()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();
        FutureContract.InvokeInstance(identity, "VerifyEmail", Now);
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AuthenticationPolicy");

        FutureContract.InvokeStatic(policyType, "CanStartInteractiveSession", identity)
            .Should().Be(false);
    }

    [Fact]
    public void VerifiedEmailAndConfiguredMfa_ShouldAllowInteractiveSessionCreation()
    {
        var identity = AccountRegistrationRedTests.CreateRegisteredIdentity();
        FutureContract.InvokeInstance(identity, "VerifyEmail", Now);
        FutureContract.InvokeInstance(
            identity,
            "ConfigureMfa",
            "protected-totp-secret",
            Now.AddMinutes(1));
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AuthenticationPolicy");

        FutureContract.InvokeStatic(policyType, "CanStartInteractiveSession", identity)
            .Should().Be(true);
    }
}
