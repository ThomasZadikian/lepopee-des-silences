using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

public sealed class RegistrationEligibilityRedTests
{
    [Fact]
    public void Registration_ShouldRequireExplicitConfirmationThatThePlayerIsAtLeastSixteen()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.MinimumAgePolicy");

        var act = () => FutureContract.InvokeStatic(policyType, "EnsureEligible", false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registration_ShouldAcceptTheMinimumAgeConfirmationWithoutStoringBirthDate()
    {
        var policyType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.MinimumAgePolicy");

        var act = () => FutureContract.InvokeStatic(policyType, "EnsureEligible", true);

        act.Should().NotThrow();
        policyType.GetProperty("BirthDate").Should().BeNull(
            "the approved design only needs an age confirmation and should not collect an unnecessary date of birth");
    }
}
