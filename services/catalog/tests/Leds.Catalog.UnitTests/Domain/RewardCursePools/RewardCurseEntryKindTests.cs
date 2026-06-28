using FluentAssertions;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.UnitTests.Domain.RewardCursePools;

public sealed class RewardCurseEntryKindTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)RewardCurseEntryKind.Reward).Should().Be(0);
        ((int)RewardCurseEntryKind.Curse).Should().Be(1);
    }

    [Fact]
    public void ShouldHaveTwoValues()
    {
        Enum.GetValues<RewardCurseEntryKind>().Should().HaveCount(2);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<RewardCurseEntryKind>("Curse");
        result.Should().Be(RewardCurseEntryKind.Curse);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        RewardCurseEntryKind.Reward.ToString().Should().Be("Reward");
    }

    [Fact]
    public void Reward_ShouldBeDefaultValue()
    {
        default(RewardCurseEntryKind).Should().Be(RewardCurseEntryKind.Reward);
    }
}
