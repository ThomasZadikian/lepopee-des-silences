using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcWoundReversibilityTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)NpcWoundReversibility.Irreversible).Should().Be(0);
        ((int)NpcWoundReversibility.SoothableByScore).Should().Be(1);
        ((int)NpcWoundReversibility.SoothableByAct).Should().Be(2);
    }

    [Fact]
    public void ShouldHaveThreeValues()
    {
        Enum.GetValues<NpcWoundReversibility>().Should().HaveCount(3);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<NpcWoundReversibility>("SoothableByAct");
        result.Should().Be(NpcWoundReversibility.SoothableByAct);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        NpcWoundReversibility.SoothableByScore.ToString().Should().Be("SoothableByScore");
    }

    [Fact]
    public void Irreversible_ShouldBeDefaultValue()
    {
        default(NpcWoundReversibility).Should().Be(NpcWoundReversibility.Irreversible);
    }
}
