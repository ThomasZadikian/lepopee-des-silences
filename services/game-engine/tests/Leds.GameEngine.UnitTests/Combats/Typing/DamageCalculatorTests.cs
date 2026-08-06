using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats.Typing;

public sealed class DamageCalculatorTests
{
    private static CombatantTypeProfile Profile(
        IEnumerable<EmotionalType>? weak = null,
        IEnumerable<EmotionalType>? resist = null,
        IEnumerable<EmotionalType>? immune = null,
        IReadOnlyCollection<EmotionalAffinityModifier>? modifiers = null)
    {
        return new CombatantTypeProfile(
            EmotionalType.Neutral,
            Enum.GetValues<EmotionalType>().ToDictionary(
                type => type,
                type => new BaseEmotionalAffinity(
                    (immune ?? []).Contains(type) ? DamageEffectiveness.Immune
                        : (weak ?? []).Contains(type) ? DamageEffectiveness.Weak
                        : (resist ?? []).Contains(type) ? DamageEffectiveness.Resistant
                        : DamageEffectiveness.Neutral,
                    (immune ?? []).Contains(type) ? 0.0
                        : (weak ?? []).Contains(type) ? 1.5
                        : (resist ?? []).Contains(type) ? 0.75
                        : 1.0)),
            modifiers);
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
    public void Local_affinity_override_wins_by_priority_without_mutating_base_profile()
    {
        var lowPriorityWeakness = EmotionalAffinityModifier.Create(
            "status.weak", EmotionalType.Rupture, DamageEffectiveness.Weak, priority: 10);
        var highPriorityImmunity = EmotionalAffinityModifier.Create(
            "equipment.immune", EmotionalType.Rupture, DamageEffectiveness.Immune, priority: 100);
        var profile = Profile(
            resist: [EmotionalType.Rupture],
            modifiers: [lowPriorityWeakness, highPriorityImmunity]);

        var outcome = DamageCalculator.Calculate(
            40, EmotionalType.Rupture, profile, critChance: 1, critRoll: 0);

        outcome.Effectiveness.Should().Be(DamageEffectiveness.Immune);
        outcome.FinalAmount.Should().Be(0);
    }

    [Fact]
    public void Local_affinity_multiplier_is_applied_after_categorical_outcome()
    {
        var modifier = EmotionalAffinityModifier.Create(
            "equipment.reduction", EmotionalType.Rupture, multiplierPercent: -20);
        var profile = Profile(weak: [EmotionalType.Rupture], modifiers: [modifier]);

        var outcome = DamageCalculator.Calculate(
            100, EmotionalType.Rupture, profile, critChance: 0, critRoll: 1);

        outcome.Effectiveness.Should().Be(DamageEffectiveness.Weak);
        outcome.FinalAmount.Should().Be(120); // 100 × 1.5 × 0.8
    }

    [Fact]
    public void Finite_affinity_modifier_expires_after_its_declared_holder_activations()
    {
        var combatant = Leds.GameEngine.Domain.Combats.Combatant.CreateAlly(
            "character.test", "Test", "Fighter", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        combatant.ApplyEmotionalAffinityModifier(EmotionalAffinityModifier.Create(
            "equipment.temporary-immunity", EmotionalType.Deni,
            DamageEffectiveness.Immune, durationActivations: 1));

        combatant.AdvanceEmotionalAffinityModifiers();

        combatant.EmotionalAffinityModifiers.Should().BeEmpty();
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
