using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

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

        enemy.CurrentVitality.Should().Be(65); // 80 - (10 * 1.5)
        result.LogEntries.Should().Contain(e => e.Type == "WeaknessHit");
    }

    [Fact]
    public void Resistance_reduces_damage_and_logs()
    {
        // Guard resists Rupture.
        var (combat, hero, enemy) = CreateCombat(enemyArchetype: "Guard");
        var skill = DamageSkill("skill.rupture", power: 10, tag: "emotype:rupture");

        var result = _resolver.Resolve(combat, hero, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(72); // 80 - round(7.5) = 80 - 8
        result.LogEntries.Should().Contain(e => e.Type == "ResistedHit");
    }

    [Fact]
    public void Neutral_attack_is_unchanged()
    {
        // Unknown hero archetype + no type tag => Neutral => x1.0, no crit (Focus 0).
        var (combat, hero, enemy) = CreateCombat(enemyArchetype: "Guard");
        var skill = DamageSkill("skill.plain", power: 10, tag: null);

        _resolver.Resolve(combat, hero, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(70);
    }

    private static (Combat Combat, Combatant Hero, Combatant Enemy) CreateCombat(string enemyArchetype)
    {
        var hero = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100); // unknown => neutral
        var enemy = Combatant.CreateEnemy("enemy.x", "Enemy", enemyArchetype, 80);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [enemy]);
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