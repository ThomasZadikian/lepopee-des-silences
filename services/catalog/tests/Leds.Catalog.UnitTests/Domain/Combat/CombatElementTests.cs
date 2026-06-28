using FluentAssertions;
using Leds.Catalog.Domain.Combat;

namespace Leds.Catalog.UnitTests.Domain.Combat;

public sealed class CombatElementTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)CombatElement.Neutral).Should().Be(0);
        ((int)CombatElement.Silence).Should().Be(1);
        ((int)CombatElement.Rupture).Should().Be(2);
        ((int)CombatElement.Memory).Should().Be(3);
        ((int)CombatElement.Fear).Should().Be(4);
        ((int)CombatElement.Grief).Should().Be(5);
        ((int)CombatElement.Anger).Should().Be(6);
        ((int)CombatElement.Light).Should().Be(7);
        ((int)CombatElement.Shadow).Should().Be(8);
    }

    [Fact]
    public void ShouldHaveNineValues()
    {
        Enum.GetValues<CombatElement>().Should().HaveCount(9);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<CombatElement>("Shadow");
        result.Should().Be(CombatElement.Shadow);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        CombatElement.Rupture.ToString().Should().Be("Rupture");
    }

    [Fact]
    public void Neutral_ShouldBeDefaultValue()
    {
        default(CombatElement).Should().Be(CombatElement.Neutral);
    }
}
