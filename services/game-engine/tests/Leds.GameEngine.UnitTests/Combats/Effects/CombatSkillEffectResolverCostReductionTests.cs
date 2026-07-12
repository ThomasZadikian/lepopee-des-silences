using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

/// <summary>
/// Mina's legendary "Protection de Him'Lit" (-5% skill mana/charge cost, permanent) —
/// see Combatant.EffectiveSkillCostReductionPercent and CombatFactory.ApplyHimLitProtection.
/// </summary>
public sealed class CombatSkillEffectResolverCostReductionTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateCombat(int manaCost, int chargeCost)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        return (combat, ally, enemy);
    }

    private static CombatantSkill CreateSkill(int manaCost, int chargeCost) =>
        CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", manaCost, chargeCost, 10);

    [Fact]
    public void Resolve_ShouldSpendFullCost_WhenActorHasNoCostReduction()
    {
        var (combat, ally, enemy) = CreateCombat(20, 10);
        var skill = CreateSkill(20, 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.Mana.Should().Be(-20);
        ally.Charge.Should().Be(-10);
    }

    [Fact]
    public void Resolve_ShouldReduceManaAndChargeCost_WhenActorHasSkillCostReduction()
    {
        var (combat, ally, enemy) = CreateCombat(20, 10);
        ally.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "test:cost-reduction", displayName: "test", kind: StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 0, magnitude: -5, stat: CombatStat.SkillCostReductionPercent,
            isPermanent: true));
        var skill = CreateSkill(20, 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // 20 * (1 - 5%) = 19, 10 * (1 - 5%) = 9.5 -> rounds to 10 (ToEven).
        ally.Mana.Should().Be(-19);
        ally.Charge.Should().Be(-10);
    }

    [Fact]
    public void Resolve_ShouldNotReduceEnemyResourceCost_RegardlessOfCostReductionStat()
    {
        var (combat, ally, enemy) = CreateCombat(0, 0);
        var skill = CreateSkill(20, 10);

        _resolver.Resolve(combat, enemy, skill, [ally]);

        enemy.Mana.Should().Be(0, because: "enemies cast freely — cost reduction only ever applies to the player side.");
    }
}
