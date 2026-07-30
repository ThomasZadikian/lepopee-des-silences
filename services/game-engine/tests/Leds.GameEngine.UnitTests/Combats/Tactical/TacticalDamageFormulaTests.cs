using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalDamageFormulaTests
{
    [Theory]
    [InlineData(0.0, 85)]
    [InlineData(0.5, 100)]
    [InlineData(1.0, 115)]
    public void CalculateBaseDamage_ShouldApplyDeterministicVariationPerTarget(
        double roll,
        int expected)
    {
        TacticalDamageFormula.CalculateBaseDamage(
                skillPower: 100,
                attack: 10,
                defense: 10,
                deterministicRoll: roll)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 115)]
    [InlineData(1, 115)]
    public void CalculateBaseDamage_ShouldIgnoreAttackRatioAndForceMaximumWhenDefenseIsZero(
        int attack,
        int expected)
    {
        TacticalDamageFormula.CalculateBaseDamage(
                skillPower: 100,
                attack,
                defense: 0,
                deterministicRoll: 0)
            .Should().Be(expected);
    }

    [Fact]
    public void CalculateBaseDamage_ShouldAllowZeroDamage()
    {
        TacticalDamageFormula.CalculateBaseDamage(
                skillPower: 100,
                attack: 0,
                defense: 10,
                deterministicRoll: 1)
            .Should().Be(0);
    }
}
