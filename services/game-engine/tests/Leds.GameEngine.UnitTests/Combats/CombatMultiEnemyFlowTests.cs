using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatMultiEnemyFlowTests
{
    private static readonly CombatantSkill StrikeSkill = CombatantSkill.Create(
        "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10);

    [Fact]
    public void Combat_ShouldSupportMultipleEnemiesInRuntime()
    {
        var allies = new[] { CreateAlly("player.self", "Hero") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B"),
            CreateEnemy("enemy.c", "Enemy C")
        };
        var combat = CreateCombat(allies, enemies);

        combat.Allies.Should().HaveCount(1);
        combat.Enemies.Should().HaveCount(3);
        combat.HasLivingEnemies.Should().BeTrue();
    }

    [Fact]
    public void PlayerAction_ShouldAllowTargetingOneEnemyAmongMany()
    {
        var allies = new[] { CreateAlly("player.self", "Hero", StrikeSkill) };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B"),
            CreateEnemy("enemy.c", "Enemy C")
        };
        var combat = CreateCombat(allies, enemies);

        var target = combat.Enemies.First(e => e.SourceKey == "enemy.b");
        target.ApplyDamage(10);

        var targetB = combat.Enemies.Single(e => e.SourceKey == "enemy.b");
        targetB.CurrentVitality.Should().Be(70);

        combat.Enemies.Where(e => e.SourceKey != "enemy.b")
            .Should().AllSatisfy(e => e.CurrentVitality.Should().Be(80));
    }

    [Fact]
    public void EnemyAutoTurns_ShouldResolveMultipleEnemiesUntilNextAlly()
    {
        var enemySkill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 5);
        var allies = new[] { CreateAlly("player.self", "Hero"), CreateAlly("player.ally", "Sidekick") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A", enemySkill),
            CreateEnemy("enemy.b", "Enemy B", enemySkill)
        };
        var combat = CreateCombat(allies, enemies);

        // Active combatant is first ally. Advance past ally1 and ally2 to reach enemy1.
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(allies[1].Id);
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(enemies[0].Id);

        // Enemy A acts
        var targetA = allies[0];
        targetA.ApplyDamage(5);
        combat.AdvanceTurn();

        // Active should now be enemy B
        combat.ActiveCombatantId.Should().Be(enemies[1].Id);

        // Enemy B acts
        targetA.ApplyDamage(5);
        combat.AdvanceTurn();

        // Now back to ally1
        combat.ActiveCombatantId.Should().Be(allies[0].Id);
    }

    [Fact]
    public void Combat_ShouldCompleteOnlyWhenAllEnemiesAreDefeated()
    {
        var lethalSkill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 80);
        var allies = new[] { CreateAlly("player.self", "Hero", lethalSkill) };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B")
        };
        var combat = CreateCombat(allies, enemies);

        combat.CompleteIfAllEnemiesDefeated();
        combat.Status.Should().Be(CombatStatus.Active);

        enemies[0].ApplyDamage(80);
        combat.CompleteIfAllEnemiesDefeated();
        combat.Status.Should().Be(CombatStatus.Active);

        enemies[1].ApplyDamage(80);
        combat.CompleteIfAllEnemiesDefeated();
        combat.Status.Should().Be(CombatStatus.Completed);
    }

    [Fact]
    public void DefeatingOneEnemy_ShouldNotCompleteCombat_WhenOtherEnemiesRemain()
    {
        var allies = new[] { CreateAlly("player.self", "Hero") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B")
        };
        var combat = CreateCombat(allies, enemies);

        enemies[0].ApplyDamage(80);

        combat.CompleteIfAllEnemiesDefeated();

        combat.Status.Should().Be(CombatStatus.Active);
        combat.HasLivingEnemies.Should().BeTrue();
    }

    [Fact]
    public void TurnProgression_ShouldSkipDefeatedEnemies()
    {
        var enemySkill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 5);
        var allies = new[] { CreateAlly("player.self", "Hero") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B", enemySkill),
            CreateEnemy("enemy.c", "Enemy C")
        };
        var combat = CreateCombat(allies, enemies);

        // Defeat enemy A before its turn
        enemies[0].ApplyDamage(80);

        // Turn order: ally, enemy A (defeated - skip), enemy B, enemy C, ally...
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(enemies[1].Id);

        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(enemies[2].Id);

        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(allies[0].Id);
    }

    [Fact]
    public void AdvancingTurn_ShouldIncrementTurnNumberOnlyWhenFullCycleCompleted()
    {
        var allies = new[] { CreateAlly("player.self", "Hero"), CreateAlly("player.ally", "Sidekick") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B")
        };
        var combat = CreateCombat(allies, enemies);

        combat.TurnNumber.Should().Be(1);

        combat.AdvanceTurn();
        combat.TurnNumber.Should().Be(1);

        combat.AdvanceTurn();
        combat.TurnNumber.Should().Be(1);

        combat.AdvanceTurn();
        combat.TurnNumber.Should().Be(1);

        combat.AdvanceTurn();
        combat.TurnNumber.Should().Be(2);
    }

    [Fact]
    public void DefeatedCombatant_CannotBeTargetedForDamage()
    {
        var allies = new[] { CreateAlly("player.self", "Hero") };
        var enemies = new[]
        {
            CreateEnemy("enemy.a", "Enemy A"),
            CreateEnemy("enemy.b", "Enemy B")
        };
        var combat = CreateCombat(allies, enemies);

        enemies[0].ApplyDamage(80);

        var act = () => enemies[0].ApplyDamage(10);
        act.Should().Throw<DomainException>().WithMessage("Defeated combatants cannot receive damage.");
    }

    [Fact]
    public void Combat_ShouldNotStart_WithoutEnemies()
    {
        var allies = new[] { CreateAlly("player.self", "Hero") };

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            Array.Empty<Combatant>());

        act.Should().Throw<DomainException>().WithMessage("*at least one enemy*");
    }

    [Fact]
    public void Combat_ShouldNotStart_WithDefeatedEnemy()
    {
        var allies = new[] { CreateAlly("player.self", "Hero") };
        var defeatedEnemy = CreateEnemy("enemy.a", "Enemy A");
        defeatedEnemy.MarkDefeated();

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            new[] { defeatedEnemy });

        act.Should().Throw<DomainException>().WithMessage("*defeated enemy*");
    }

    private static Combat CreateCombat(
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies)
    {
        return Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);
    }

    private static Combatant CreateAlly(string sourceKey, string displayName, params CombatantSkill[] skills)
    {
        return Combatant.CreateAlly(sourceKey, displayName, "Fighter", 100, 0,
            skills.Length > 0 ? skills : Array.Empty<CombatantSkill>());
    }

    private static Combatant CreateEnemy(string sourceKey, string displayName, params CombatantSkill[] skills)
    {
        return Combatant.CreateEnemy(sourceKey, displayName, "Guard", 80,
            skills.Length > 0 ? skills : Array.Empty<CombatantSkill>());
    }
}
