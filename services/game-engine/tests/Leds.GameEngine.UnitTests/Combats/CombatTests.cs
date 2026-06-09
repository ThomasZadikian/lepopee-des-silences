using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatTests
{
    private static Combat CreateSut(int allyCount = 1, int enemyCount = 1)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            Combatant.CreateAlly($"player.{i}", $"Hero{i}", "Fighter", 100)).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
            Combatant.CreateEnemy($"enemy.{i}", $"Enemy{i}", "Guard", 80)).ToArray();

        return Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);
    }

    [Fact]
    public void Create_ShouldSucceed_WithOneAllyAndOneEnemy()
    {
        var combat = CreateSut();

        combat.Allies.Should().HaveCount(1);
        combat.Enemies.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ShouldSucceed_WithMultipleAlliesAndEnemies()
    {
        var combat = CreateSut(allyCount: 3, enemyCount: 2);

        combat.Allies.Should().HaveCount(3);
        combat.Enemies.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoAlly()
    {
        var enemies = new[] { Combatant.CreateEnemy("enemy.0", "Enemy", "Guard", 80) };

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            Array.Empty<Combatant>(),
            enemies);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one ally.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoEnemy()
    {
        var allies = new[] { Combatant.CreateAlly("player.self", "Hero", "Fighter", 100) };

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            Array.Empty<Combatant>());

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one enemy.");
    }

    [Fact]
    public void Create_ShouldStartAsActive()
    {
        var combat = CreateSut();

        combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public void Create_ShouldStartAtTurnOne()
    {
        var combat = CreateSut();

        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldSetFirstAllyAsActiveCombatant()
    {
        var allies = new[]
        {
            Combatant.CreateAlly("player.1", "Alice", "Fighter", 100),
            Combatant.CreateAlly("player.2", "Bob", "Mage", 80)
        };
        var enemies = new[] { Combatant.CreateEnemy("enemy.0", "Enemy", "Guard", 80) };

        var combat = Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);

        combat.ActiveCombatantId.Should().Be(allies[0].Id);
    }

    [Fact]
    public void Create_ShouldRejectCombatantWithInvalidVitality()
    {
        var allies = new[] { Combatant.CreateAlly("player.self", "Hero", "Fighter", 100) };

        var act = () =>
        {
            var enemies = new[]
            {
                Combatant.Create(
                    CombatantId.New(),
                    "enemy.0",
                    "Enemy",
                    CombatantSide.Enemy,
                    "Guard",
                    maxVitality: -10,
                    currentVitality: -10,
                    guard: 0,
                    mana: 0,
                    charge: 0)
            };

            Combat.Create(
                CombatId.New(),
                RunId.New(),
                RoomId.New(),
                NodeId.New(),
                allies,
                enemies);
        };

        act.Should().Throw<DomainException>().WithMessage("Combatant max vitality must be greater than zero.");
    }

    [Fact]
    public void MarkCompleted_ShouldSetStatusCompleted()
    {
        var combat = CreateSut();

        combat.MarkCompleted();

        combat.Status.Should().Be(CombatStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_ShouldThrow_WhenNotActive()
    {
        var combat = CreateSut();
        combat.MarkCompleted();

        var act = () => combat.MarkCompleted();

        act.Should().Throw<DomainException>().WithMessage("Only an active combat can be completed.");
    }

    [Fact]
    public void MarkFailed_ShouldSetStatusFailed()
    {
        var combat = CreateSut();

        combat.MarkFailed();

        combat.Status.Should().Be(CombatStatus.Failed);
    }

    [Fact]
    public void MarkFailed_ShouldThrow_WhenNotActive()
    {
        var combat = CreateSut();
        combat.MarkCompleted();

        var act = () => combat.MarkFailed();

        act.Should().Throw<DomainException>().WithMessage("Only an active combat can be failed.");
    }
}
