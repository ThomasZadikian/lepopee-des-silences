using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

public sealed class CombatSkillEffectResolverTypingTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    [Fact]
    public void Weakness_amplifies_damage_and_logs()
    {
        // Fragile enemy is weak to Rupture; hero attacks with a Rupture-tagged skill.
        var (combat, hero, enemy) = CreateCombat(enemyArchetype: "Fragile");
        var skill = DamageSkill("skill.rupture", power: 10, tag: "emotype:rupture");

        var result = _resolver.Resolve(combat, hero, skill, [enemy]);

        // enemy.EffectiveDefense is 0, so TacticalDamageFormula forces
        // round(10 * 1.15) = 12 as the base, then the x1.5 weakness multiplier: 18.
        enemy.CurrentVitality.Should().Be(80 - 18);
        result.LogEntries.Should().Contain(e => e.Type == "WeaknessHit");
    }

    [Fact]
    public void Resistance_reduces_damage_and_logs()
    {
        // Guard resists Rupture.
        var (combat, hero, enemy) = CreateCombat(enemyArchetype: "Guard");
        var skill = DamageSkill("skill.rupture", power: 10, tag: "emotype:rupture");

        var result = _resolver.Resolve(combat, hero, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0), then the x0.75 resistance
        // multiplier: round(12 * 0.75) = 9.
        enemy.CurrentVitality.Should().Be(80 - 9);
        result.LogEntries.Should().Contain(e => e.Type == "ResistedHit");
    }

    [Fact]
    public void Neutral_attack_is_unchanged()
    {
        // Unknown hero archetype + no type tag => Neutral => x1.0, no crit (Focus 0).
        var (combat, hero, enemy) = CreateCombat(enemyArchetype: "Guard");
        var skill = DamageSkill("skill.plain", power: 10, tag: null);

        _resolver.Resolve(combat, hero, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0), neutral type match => unchanged.
        enemy.CurrentVitality.Should().Be(68);
    }

    private static (TacticalCombat Combat, Combatant Hero, Combatant Enemy) CreateCombat(string enemyArchetype)
    {
        var hero = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100); // unknown => neutral
        // Guaranteed hit chance: the tactical resolver rolls a real hit/miss chance
        // (baseline 90%) per attack that the old ATB-era fixture this file was
        // originally written against never had — pin it so these single-shot damage
        // assertions aren't ~10% flaky.
        hero.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.x", "Enemy", enemyArchetype, 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [enemy]);
        return (combat, hero, enemy);
    }

    private static CombatantSkill DamageSkill(string key, int power, string? tag)
    {
        return CombatantSkill.Create(
            key,
            key,
            "Damage",
            "SingleEnemy",
            "Damage",
            0,
            0,
            power,
            tag is null ? null : new[] { tag });
    }
}