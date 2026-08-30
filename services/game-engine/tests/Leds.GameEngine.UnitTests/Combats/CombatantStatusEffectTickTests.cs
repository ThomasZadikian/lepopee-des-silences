using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantStatusEffectTickTests
{
    [Fact]
    public void TickStatusEffects_ShouldGrantGuard_ForGuardOverTimeEffect()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", maxVitality: 100);
        combatant.ApplyStatusEffect(CombatStatusEffect.Create(
            "construction-perpetuelle:guardovertime", "Construction perpétuelle", StatusEffectKind.GuardOverTime,
            currentTick: 0, durationTicks: 6000, magnitude: 8, tickInterval: 1400));

        var guardBefore = combatant.Guard;

        var events = combatant.TickStatusEffects(1400);

        combatant.Guard.Should().Be(guardBefore + 8);
        events.Should().ContainSingle(e => e.Kind == StatusEffectKind.GuardOverTime && e.Amount == 8);
    }

    [Fact]
    public void TickStatusEffects_ShouldHealPercentOfMaxVitality_WhenEffectIsPercentBased()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", maxVitality: 200);
        combatant.ApplyDamage(150); // drop well below max so the heal isn't clamped

        combatant.ApplyStatusEffect(CombatStatusEffect.Create(
            "construction-perpetuelle:healovertime", "Construction perpétuelle", StatusEffectKind.HealOverTime,
            currentTick: 0, durationTicks: 6000, magnitude: 10, tickInterval: 1400,
            isMagnitudePercentOfMax: true));

        var vitalityBefore = combatant.CurrentVitality;

        combatant.TickStatusEffects(1400);

        // 10% of 200 max vitality = 20.
        combatant.CurrentVitality.Should().Be(vitalityBefore + 20);
    }

    [Fact]
    public void ApplyTypedDamageReductions_ShouldBeReadableFromCombatant()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", maxVitality: 100);

        combatant.ApplyTypedDamageReductions(new Dictionary<EmotionalType, int> { [EmotionalType.Memoire] = 15 });

        combatant.TypedDamageReductionPercent.Should().ContainKey(EmotionalType.Memoire)
            .WhoseValue.Should().Be(15);
    }

    [Fact]
    public void ApplyTypedDamageReductions_ShouldClearWhenNull()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", maxVitality: 100);
        combatant.ApplyTypedDamageReductions(new Dictionary<EmotionalType, int> { [EmotionalType.Memoire] = 15 });

        combatant.ApplyTypedDamageReductions(null);

        combatant.TypedDamageReductionPercent.Should().BeEmpty();
    }

    [Fact]
    public void EffectiveAttackPower_ShouldApplyPercentBasedStatModifier_AgainstBaseValue()
    {
        var combatant = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            attackPower: 12);

        combatant.ApplyStatusEffect(CombatStatusEffect.Create(
            "bague-du-courage:attack", "Bague du courage", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 6000, magnitude: 10, stat: CombatStat.AttackPower,
            isMagnitudePercentOfBaseStat: true));

        // 12 base + round(12 * 10%) = 13.
        combatant.EffectiveAttackPower.Should().Be(13);
    }

    [Fact]
    public void EffectiveSpeed_ShouldCombineFlatAndPercentStatModifiers_Independently()
    {
        var combatant = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 10);

        combatant.ApplyStatusEffect(CombatStatusEffect.Create(
            "flat-speed-buff", "Flat", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 6000, magnitude: 2, stat: CombatStat.Speed));
        combatant.ApplyStatusEffect(CombatStatusEffect.Create(
            "percent-speed-buff", "Percent", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 6000, magnitude: 10, stat: CombatStat.Speed,
            isMagnitudePercentOfBaseStat: true));

        // 10 base + 2 flat + round(10 * 10%) = 13.
        combatant.EffectiveSpeed.Should().Be(13);
    }
}
