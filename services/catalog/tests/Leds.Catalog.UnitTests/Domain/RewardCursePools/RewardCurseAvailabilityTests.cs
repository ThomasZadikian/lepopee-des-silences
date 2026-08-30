using FluentAssertions;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.UnitTests.Domain.RewardCursePools;

public sealed class RewardCurseAvailabilityTests
{
    [Fact]
    public void Create_ShouldCreateAvailability_WithAllProperties()
    {
        var availability = new RewardCurseAvailability(
            Kind: RewardCursePoolFilterKind.MinNodeDepth,
            Value: 5);

        availability.Kind.Should().Be(RewardCursePoolFilterKind.MinNodeDepth);
        availability.Value.Should().Be(5);
    }

    [Fact]
    public void Create_ShouldCreateAvailability_WithZeroValue()
    {
        var availability = new RewardCurseAvailability(
            Kind: RewardCursePoolFilterKind.MinActiveLawCount,
            Value: 0);

        availability.Value.Should().Be(0);
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var a1 = new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 5);
        var a2 = new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 5);

        a1.Should().Be(a2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenKindDiffers()
    {
        var a1 = new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 5);
        var a2 = new RewardCurseAvailability(RewardCursePoolFilterKind.MaxVitalityRatioPercent, 5);

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenValueDiffers()
    {
        var a1 = new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 5);
        var a2 = new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 10);

        a1.Should().NotBe(a2);
    }
}
