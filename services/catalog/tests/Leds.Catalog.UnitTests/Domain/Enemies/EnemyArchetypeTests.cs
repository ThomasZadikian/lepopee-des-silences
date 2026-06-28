using FluentAssertions;
using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.UnitTests.Domain.Enemies;

public sealed class EnemyArchetypeTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)EnemyArchetype.Unknown).Should().Be(0);
        ((int)EnemyArchetype.Trauma).Should().Be(1);
        ((int)EnemyArchetype.Memory).Should().Be(2);
        ((int)EnemyArchetype.Shadow).Should().Be(3);
        ((int)EnemyArchetype.Guardian).Should().Be(4);
        ((int)EnemyArchetype.Elite).Should().Be(5);
        ((int)EnemyArchetype.Boss).Should().Be(6);
    }

    [Fact]
    public void ShouldHaveSevenValues()
    {
        Enum.GetValues<EnemyArchetype>().Should().HaveCount(7);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<EnemyArchetype>("Boss");
        result.Should().Be(EnemyArchetype.Boss);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        EnemyArchetype.Shadow.ToString().Should().Be("Shadow");
    }

    [Fact]
    public void Unknown_ShouldBeDefaultValue()
    {
        default(EnemyArchetype).Should().Be(EnemyArchetype.Unknown);
    }
}
