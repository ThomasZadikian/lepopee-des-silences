using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

public sealed class BossBehaviorTests
{
    private static CombatantSkill CreateSkill(string key, string effectType, string targetingType, int basePower)
    {
        return CombatantSkill.Create(
            key,
            key,
            effectType,
            targetingType,
            effectType,
            0,
            0,
            basePower);
    }

    // Regression test: LowestHpPlayer/HighestHpPlayer used a stable OrderBy, so
    // equal-HP ties always resolved to the first ally (the protagonist). They now
    // break ties with a deterministic per-candidate hash instead.
    [Fact]
    public void GrandCardinal_ShouldNotAlwaysTargetTheFirstAlly_WhenAlliesAreTiedAtFullHealth()
    {
        var distinctTargets = new HashSet<Guid>();

        for (var i = 0; i < 20; i++)
        {
            var drain = CreateSkill("canon.skill.priere-aspiration", "Drain", "SingleEnemy", 5);
            var strike = CreateSkill("canon.skill.flamme-froide", "Damage", "SingleEnemy", 5);
            var ally1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
            var ally2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
            var boss = Combatant.CreateEnemy("canon.enemy.grand-cardinal", "Le Grand Cardinal", "Boss", 200, [drain, strike]);
            var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally1, ally2], [boss]);

            var decision = new GrandCardinalBossBehavior().DecideAction(new BossDecisionContext(combat, boss));

            distinctTargets.Add(decision!.TargetIds.Single());
        }

        distinctTargets.Should().HaveCountGreaterThan(1,
            "ties between equally-untouched allies must not always resolve to the same combatant");
    }
}
