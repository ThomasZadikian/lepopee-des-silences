using FluentAssertions;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.UnitTests.Domain.RewardCursePools;

public sealed class RewardCursePoolFilterKindTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)RewardCursePoolFilterKind.MinVitalityRatioPercent).Should().Be(0);
        ((int)RewardCursePoolFilterKind.MaxVitalityRatioPercent).Should().Be(1);
        ((int)RewardCursePoolFilterKind.MinActiveLawCount).Should().Be(2);
        ((int)RewardCursePoolFilterKind.MinNodeDepth).Should().Be(3);
    }

    [Fact]
    public void ShouldHaveFourValues()
    {
        Enum.GetValues<RewardCursePoolFilterKind>().Should().HaveCount(4);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<RewardCursePoolFilterKind>("MinNodeDepth");
        result.Should().Be(RewardCursePoolFilterKind.MinNodeDepth);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        RewardCursePoolFilterKind.MinVitalityRatioPercent.ToString().Should().Be("MinVitalityRatioPercent");
    }

    [Fact]
    public void MinVitalityRatioPercent_ShouldBeDefaultValue()
    {
        default(RewardCursePoolFilterKind).Should().Be(RewardCursePoolFilterKind.MinVitalityRatioPercent);
    }
}
