using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats.Typing;

public sealed class DamageCalculatorTests
{
    private static CombatantTypeProfile Profile(
        IEnumerable<EmotionalType>? weak = null,
        IEnumerable<EmotionalType>? resist = null,
        IEnumerable<EmotionalType>? immune = null)
    {
        return new CombatantTypeProfile(
            EmotionalType.Neutral,
            new HashSet<EmotionalType>(weak ?? Array.Empty<EmotionalType>()),
            new HashSet<EmotionalType>(resist ?? Array.Empty<EmotionalType>()),
            new HashSet<EmotionalType>(immune ?? Array.Empty<EmotionalType>()));
    }

    [Fact]
    public void Neutral_effectiveness_returns_base_power()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Rupture, Profile(), critChance: 0, critRoll: 0.99);

        outcome.FinalAmount.Should().Be(10);
        outcome.Effectiveness.Should().Be(DamageEffectiveness.Neutral);
        outcome.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Weakness_amplifies_by_one_and_a_half()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Rupture, Profile(weak: [EmotionalType.Rupture]), 0, 0.99);

        outcome.FinalAmount.Should().Be(15);
        outcome.Effectiveness.Should().Be(DamageEffectiveness.Weak);
    }

    [Fact]
    public void Resistance_reduces_to_three_quarters()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Rupture, Profile(resist: [EmotionalType.Rupture]), 0, 0.99);

        outcome.FinalAmount.Should().Be(8); // 7.5 rounded away from zero
        outcome.Effectiveness.Should().Be(DamageEffectiveness.Resistant);
    }

    [Fact]
    public void Immunity_zeroes_damage_and_cannot_crit()
    {
        var outcome = DamageCalculator.Calculate(40, EmotionalType.Rupture, Profile(immune: [EmotionalType.Rupture]), critChance: 1.0, critRoll: 0.0);

        outcome.FinalAmount.Should().Be(0);
        outcome.Effectiveness.Should().Be(DamageEffectiveness.Immune);
        outcome.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void Critical_applies_when_roll_below_chance()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Neutral, Profile(), critChance: 0.5, critRoll: 0.1);

        outcome.IsCritical.Should().BeTrue();
        outcome.FinalAmount.Should().Be(15); // 10 * 1.5
    }

    [Fact]
    public void Critical_stacks_with_weakness()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Rupture, Profile(weak: [EmotionalType.Rupture]), critChance: 0.5, critRoll: 0.1);

        outcome.IsCritical.Should().BeTrue();
        outcome.FinalAmount.Should().Be(23); // 10 * 1.5 * 1.5 = 22.5 -> 23
    }

    [Fact]
    public void No_critical_when_roll_at_or_above_chance()
    {
        var outcome = DamageCalculator.Calculate(10, EmotionalType.Neutral, Profile(), critChance: 0.5, critRoll: 0.5);

        outcome.IsCritical.Should().BeFalse();
        outcome.FinalAmount.Should().Be(10);
    }

    [Fact]
    public void Non_immune_hit_floors_at_one()
    {
        var outcome = DamageCalculator.Calculate(1, EmotionalType.Rupture, Profile(resist: [EmotionalType.Rupture]), 0, 0.99);

        outcome.FinalAmount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Crit_chance_from_focus_scales_and_caps()
    {
        CriticalHitCalibration.CritChanceFromFocus(0).Should().Be(0.0);
        CriticalHitCalibration.CritChanceFromFocus(20).Should().BeApproximately(0.20, 1e-9);
        CriticalHitCalibration.CritChanceFromFocus(1000).Should().Be(CriticalHitCalibration.MaxCritChance);
    }

    [Fact]
    public void Deterministic_roll_is_stable_and_in_range()
    {
        var a = DeterministicCombatRoll.UnitInterval("seed-x");
        var b = DeterministicCombatRoll.UnitInterval("seed-x");

        a.Should().Be(b);
        a.Should().BeInRange(0, 1);
        DeterministicCombatRoll.UnitInterval("seed-y").Should().NotBe(a);
    }
}