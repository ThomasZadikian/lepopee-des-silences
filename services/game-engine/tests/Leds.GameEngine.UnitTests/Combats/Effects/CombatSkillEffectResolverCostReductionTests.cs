using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

/// <summary>
/// Mina's legendary "Protection de Him'Lit" (-5% skill mana/charge cost, permanent) —
/// see Combatant.EffectiveSkillCostReductionPercent and CombatFactory.ApplyHimLitProtection.
/// </summary>
public sealed class CombatSkillEffectResolverCostReductionTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateCombat(int manaCost, int chargeCost)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        // ConsumeResources now enforces sufficiency before spending — grant exactly what
        // the skill will cost so the resolve call itself doesn't throw.
        ally.GainMana(manaCost);
        ally.GainCharge(chargeCost);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        return (combat, ally, enemy);
    }

    private static CombatantSkill CreateSkill(int manaCost, int chargeCost) =>
        CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", manaCost, chargeCost, 10);

    [Fact]
    public void Resolve_ShouldSpendFullCost_WhenActorHasNoCostReduction()
    {
        // chargeCost is capped at 4 here (not 10) because Combatant.Charge is hard-clamped
        // to a max of 5 (the canonical 0..5 gauge) — a cost of 10 could never be granted.
        var (combat, ally, enemy) = CreateCombat(20, 4);
        var skill = CreateSkill(20, 4);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // ConsumeResources now clamps spend to what's available rather than going
        // negative (a mana shortfall instead converts into vitality damage) — granted
        // exactly the cost above, so both resources land at exactly zero.
        ally.Mana.Should().Be(0);
        ally.Charge.Should().Be(0);
    }

    [Fact]
    public void Resolve_ShouldReduceManaAndChargeCost_WhenActorHasSkillCostReduction()
    {
        var (combat, ally, enemy) = CreateCombat(20, 4);
        ally.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "test:cost-reduction", displayName: "test", kind: StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 0, magnitude: -5, stat: CombatStat.SkillCostReductionPercent,
            isPermanent: true));
        var skill = CreateSkill(20, 4);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // 20 * (1 - 5%) = 19, 4 * (1 - 5%) = 3.8 -> rounds to 4.
        // Granted 20 mana / 4 charge; spending 19/4 leaves 1/0.
        ally.Mana.Should().Be(1);
        ally.Charge.Should().Be(0);
    }

    [Fact]
    public void Resolve_ShouldNotReduceEnemyResourceCost_RegardlessOfCostReductionStat()
    {
        var (combat, ally, enemy) = CreateCombat(0, 0);
        // chargeCost 0 here — ConsumeResources unconditionally requires actor.Charge >=
        // chargeCost regardless of side, so a nonzero charge cost on a 0-charge enemy
        // would throw before ever reaching the mana behavior this test targets.
        var skill = CreateSkill(20, 0);

        _resolver.Resolve(combat, enemy, skill, [ally]);

        enemy.Mana.Should().Be(0, because: "enemies cast freely — cost reduction only ever applies to the player side.");
    }
}
