using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Atb;

namespace Leds.GameEngine.UnitTests.Combats.Atb;

public sealed class AtbTempoFormulaTests
{
    [Fact]
    public void ComputeFillPerTick_ShouldEqualSpeed_WhenEverythingIsNeutral()
    {
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 10,
            effectiveAttackPower: 0,
            effectiveDefense: 0,
            opponentAverageEffectiveSpeed: 10,
            roomFactorPerMille: 1000,
            combatantFactorPerMille: 1000,
            tempoMomentumPerMille: 0);

        fill.Should().Be(10);
    }

    [Fact]
    public void ComputeFillPerTick_ShouldSlowDown_WhenAttackAndDefenseAreAboveBaseline()
    {
        // Baseline is 20 (same idiom as the damage formula); Attack 30 + Defense 30 = 60,
        // 20 above (2×baseline=40) -> investment = 20 / (20 + 20) = 0.5.
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 100,
            effectiveAttackPower: 30,
            effectiveDefense: 30,
            opponentAverageEffectiveSpeed: 100,
            roomFactorPerMille: 1000,
            combatantFactorPerMille: 1000,
            tempoMomentumPerMille: 0);

        fill.Should().Be(50);
    }

    [Fact]
    public void ComputeFillPerTick_ShouldSpeedUp_WhenFasterThanOpponentAverage()
    {
        // (100 - 95) / 20 = +0.25 relative bonus -> ×1.25.
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 100,
            effectiveAttackPower: 0,
            effectiveDefense: 0,
            opponentAverageEffectiveSpeed: 95,
            roomFactorPerMille: 1000,
            combatantFactorPerMille: 1000,
            tempoMomentumPerMille: 0);

        fill.Should().Be(125);
    }

    [Fact]
    public void ComputeFillPerTick_ShouldClampRelativeAdvantage_AtFiftyPercent()
    {
        // Enormous speed gap would exceed +50% uncapped; clamp keeps it at ×1.5.
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 1000,
            effectiveAttackPower: 0,
            effectiveDefense: 0,
            opponentAverageEffectiveSpeed: 10,
            roomFactorPerMille: 1000,
            combatantFactorPerMille: 1000,
            tempoMomentumPerMille: 0);

        fill.Should().Be(1500);
    }

    [Fact]
    public void ComputeFillPerTick_ShouldApplyMomentum_AsAdditiveBonus()
    {
        // 200 per-mille momentum -> ×1.2.
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 100,
            effectiveAttackPower: 0,
            effectiveDefense: 0,
            opponentAverageEffectiveSpeed: 100,
            roomFactorPerMille: 1000,
            combatantFactorPerMille: 1000,
            tempoMomentumPerMille: 200);

        fill.Should().Be(120);
    }

    [Fact]
    public void ComputeFillPerTick_ShouldNeverDropBelowOne()
    {
        var fill = AtbTempoFormula.ComputeFillPerTick(
            effectiveSpeed: 1,
            effectiveAttackPower: 1000,
            effectiveDefense: 1000,
            opponentAverageEffectiveSpeed: 1,
            roomFactorPerMille: 800,
            combatantFactorPerMille: 800,
            tempoMomentumPerMille: 0);

        fill.Should().BeGreaterThanOrEqualTo(1);
    }
}
