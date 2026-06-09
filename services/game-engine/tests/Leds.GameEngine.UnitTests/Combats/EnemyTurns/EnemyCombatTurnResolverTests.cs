using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Leds.GameEngine.Infrastructure.Combats.Targeting;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

public sealed class EnemyCombatTurnResolverTests
{
    private readonly EnemyCombatTurnResolver _resolver = new(
        new CombatSkillActionValidator(new CombatTargetingRuleValidator()),
        new CombatSkillEffectResolver());

    [Fact]
    public void Resolve_ShouldDoNothing_WhenCombatIsNotActive()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.MarkCompleted();

        var result = _resolver.Resolve(combat);

        result.WasResolved.Should().BeFalse();
        result.Combat.Status.Should().Be(CombatStatus.Completed);
    }

    [Fact]
    public void Resolve_ShouldDoNothing_WhenActiveCombatantIsAlly()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);

        var result = _resolver.Resolve(combat);

        result.WasResolved.Should().BeFalse();
        result.Combat.ActiveCombatantId.Should().Be(combat.Allies.Single().Id);
    }

    [Fact]
    public void Resolve_ShouldResolveEnemyTurn_WhenActiveCombatantIsEnemy()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.WasResolved.Should().BeTrue();
        result.ActorId.Should().Be(combat.Enemies.Single().Id.Value);
        result.SkillKey.Should().Be("skill.basic.strike");
    }

    [Fact]
    public void Resolve_ShouldSelectFirstOffensiveSkill()
    {
        var laterSkill = CreateSkill("skill.z.guard", "Guard", "Self", 5);
        var firstOffensive = CreateSkill("skill.a.strike", "Damage", "SingleEnemy", 10);
        var combat = CreateCombat(enemySkills: [laterSkill, firstOffensive]);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.SkillKey.Should().Be("skill.a.strike");
    }

    [Fact]
    public void Resolve_ShouldTargetFirstActiveAlly()
    {
        var skill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var ally2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        ally1.MarkDefeated();
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy", "Guard", 80, [skill]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally2], [enemy]);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.TargetIds.Should().ContainSingle(id => id == ally2.Id.Value);
    }

    [Fact]
    public void Resolve_ShouldApplySkillEffect()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.AdvanceTurn();
        var ally = combat.Allies.Single();

        _resolver.Resolve(combat);

        ally.CurrentVitality.Should().Be(90);
    }

    [Fact]
    public void Resolve_ShouldAdvanceTurnAfterEnemyAction()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.Combat.ActiveCombatantId.Should().Be(combat.Allies.Single().Id);
        result.Combat.TurnNumber.Should().Be(2);
    }

    [Fact]
    public void Resolve_ShouldSkipDefeatedAllies_WhenSelectingTarget()
    {
        var skill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var ally2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy", "Guard", 80, [skill]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally1, ally2], [enemy]);
        ally1.MarkDefeated();
        combat.AdvanceTurn();
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.TargetIds.Should().ContainSingle(id => id == ally2.Id.Value);
        ally2.CurrentVitality.Should().Be(90);
    }

    [Fact]
    public void Resolve_ShouldMarkCombatFailed_WhenNoActiveAllyExists()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.AdvanceTurn();
        combat.Allies.Single().MarkDefeated();

        var result = _resolver.Resolve(combat);

        result.Combat.Status.Should().Be(CombatStatus.Failed);
        result.LogEntries.Should().Contain(e => e.Type == "CombatFailed");
    }

    [Fact]
    public void Resolve_ShouldAdvanceTurn_WhenEnemyHasNoSkill()
    {
        var combat = CreateCombat(enemySkills: []);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.WasResolved.Should().BeTrue();
        result.Combat.ActiveCombatantId.Should().Be(combat.Allies.Single().Id);
        result.LogEntries.Should().Contain(e => e.Message.Contains("no usable skill"));
    }

    [Fact]
    public void Resolve_ShouldBeDeterministic_ForSameCombatState()
    {
        var combat1 = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        var combat2 = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat1.AdvanceTurn();
        combat2.AdvanceTurn();

        var result1 = _resolver.Resolve(combat1);
        var result2 = _resolver.Resolve(combat2);

        result1.SkillKey.Should().Be(result2.SkillKey);
        result1.LogEntries.Select(e => e.Type).Should().Equal(result2.LogEntries.Select(e => e.Type));
    }

    [Fact]
    public void Resolve_ShouldReturnLogs()
    {
        var combat = CreateCombat(enemySkills: [CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10)]);
        combat.AdvanceTurn();

        var result = _resolver.Resolve(combat);

        result.LogEntries.Should().Contain(e => e.Type == "EnemyTurnResolved");
        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Type == "TurnAdvanced");
    }

    private static Combat CreateCombat(IReadOnlyCollection<CombatantSkill> enemySkills)
    {
        var ally = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy", "Guard", 80, enemySkills);

        return Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
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
}
