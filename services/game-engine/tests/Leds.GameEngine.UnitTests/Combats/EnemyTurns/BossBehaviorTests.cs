using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Leds.GameEngine.Infrastructure.Combats.Targeting;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

public sealed class BossBehaviorTests
{
    private static EnemyCombatTurnResolver CreateResolver(params IBossBehavior[] behaviors)
    {
        return new EnemyCombatTurnResolver(
            new CombatSkillActionValidator(new CombatTargetingRuleValidator()),
            new CombatSkillEffectResolver(),
            behaviors);
    }

    [Fact]
    public void Resolve_ShouldUseScriptedSkill_WhenBehaviorMatchesBossSourceKey()
    {
        // The generic AI orders skills alphabetically and would pick "skill.a.strike".
        // The behavior overrides that to play the special "skill.z.crush".
        var weak = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 5);
        var special = CreateSkill("skill.z.crush", "Damage", "SingleEnemy", 40);
        var combat = CreateBossCombat("boss.memory.archivist", [weak, special], out var hero);
        combat.AdvanceTurn();

        var resolver = CreateResolver(new ScriptedBossBehavior(
            "boss.memory.archivist",
            ctx => new BossActionDecision("skill.z.crush", [hero.Id.Value])));

        var result = resolver.Resolve(combat);

        result.WasResolved.Should().BeTrue();
        result.SkillKey.Should().Be("skill.z.crush");
        hero.CurrentVitality.Should().Be(60);
    }

    [Fact]
    public void Resolve_ShouldFallBackToGenericAi_WhenSourceKeyHasNoBehavior()
    {
        var weak = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 5);
        var special = CreateSkill("skill.z.crush", "Damage", "SingleEnemy", 40);
        var combat = CreateBossCombat("enemy.plain.guard", [weak, special], out _);
        combat.AdvanceTurn();

        // Behavior is keyed to a different boss; this enemy must use the generic AI.
        var resolver = CreateResolver(new ScriptedBossBehavior(
            "boss.memory.archivist",
            _ => new BossActionDecision("skill.z.crush", [])));

        var result = resolver.Resolve(combat);

        result.SkillKey.Should().Be("skill.a.strike");
    }

    [Fact]
    public void Resolve_ShouldFallBackToGenericAi_WhenBehaviorDeclinesTurn()
    {
        var weak = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 5);
        var combat = CreateBossCombat("boss.memory.archivist", [weak], out _);
        combat.AdvanceTurn();

        // DecideAction returns null → defer this turn to the generic AI.
        var resolver = CreateResolver(new ScriptedBossBehavior(
            "boss.memory.archivist",
            _ => null));

        var result = resolver.Resolve(combat);

        result.WasResolved.Should().BeTrue();
        result.SkillKey.Should().Be("skill.a.strike");
    }

    [Fact]
    public void Resolve_ShouldFallBackToGenericAi_WhenScriptedSkillIsNotOwnedByBoss()
    {
        var weak = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 5);
        var combat = CreateBossCombat("boss.memory.archivist", [weak], out _);
        combat.AdvanceTurn();

        // The decision references a skill the boss does not own → graceful fallback.
        var resolver = CreateResolver(new ScriptedBossBehavior(
            "boss.memory.archivist",
            _ => new BossActionDecision("skill.unknown.nope", [])));

        var result = resolver.Resolve(combat);

        result.SkillKey.Should().Be("skill.a.strike");
    }

    [Fact]
    public void Resolve_ShouldMatchSourceKeyCaseInsensitively()
    {
        var weak = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 5);
        var special = CreateSkill("skill.z.crush", "Damage", "SingleEnemy", 40);
        var combat = CreateBossCombat("Boss.Memory.Archivist", [weak, special], out var hero);
        combat.AdvanceTurn();

        var resolver = CreateResolver(new ScriptedBossBehavior(
            "boss.memory.archivist",
            _ => new BossActionDecision("skill.z.crush", [hero.Id.Value])));

        var result = resolver.Resolve(combat);

        result.SkillKey.Should().Be("skill.z.crush");
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
            var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally1, ally2], [boss]);
            combat.AdvanceTurn();

            var resolver = CreateResolver(new GrandCardinalBossBehavior());
            var result = resolver.Resolve(combat);

            distinctTargets.Add(result.TargetIds.Single());
        }

        distinctTargets.Should().HaveCountGreaterThan(1,
            "ties between equally-untouched allies must not always resolve to the same combatant");
    }

    private static Combat CreateBossCombat(
        string bossSourceKey,
        IReadOnlyCollection<CombatantSkill> bossSkills,
        out Combatant hero)
    {
        hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var boss = Combatant.CreateEnemy(bossSourceKey, "Boss", "Boss", 200, bossSkills);

        return Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [boss]);
    }

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

    private sealed class ScriptedBossBehavior : IBossBehavior
    {
        private readonly Func<BossDecisionContext, BossActionDecision?> _decide;

        public ScriptedBossBehavior(string bossKey, Func<BossDecisionContext, BossActionDecision?> decide)
        {
            BossKey = bossKey;
            _decide = decide;
        }

        public string BossKey { get; }

        public BossActionDecision? DecideAction(BossDecisionContext context) => _decide(context);
    }
}
