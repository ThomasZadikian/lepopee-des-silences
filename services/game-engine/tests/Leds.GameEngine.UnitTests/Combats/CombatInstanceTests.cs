using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatInstanceTests
{
    [Fact]
    public void Create_ShouldStartCombatInProgress()
    {
        var player = CreatePlayer(speed: 10);
        var enemy = CreateEnemy(speed: 5);

        var combat = CombatInstance.Create(new[] { player, enemy });

        combat.State.Should().Be(CombatState.InProgress);
        combat.Round.Should().Be(1);
        combat.CurrentActorId.Should().Be(player.Id);
        combat.Combatants.Should().HaveCount(2);
    }

    [Fact]
    public void SubmitAction_ShouldApplyDamage_AndAdvanceTurn()
    {
        var player = CreatePlayer(speed: 10, attack: 12);
        var enemy = CreateEnemy(speed: 5, defense: 3, health: 30);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var result = combat.SubmitAction(
            CombatAction.BasicAttack(player.Id, enemy.Id));

        result.Damage.Should().Be(9);
        result.TargetRemainingHealth.Should().Be(21);
        result.TargetDefeated.Should().BeFalse();
        result.CombatState.Should().Be(CombatState.InProgress);
        result.NextActorId.Should().Be(enemy.Id);
        combat.CurrentActorId.Should().Be(enemy.Id);
    }

    [Fact]
    public void SubmitAction_ShouldCompleteCombat_WhenAllEnemiesAreDefeated()
    {
        var player = CreatePlayer(speed: 10, attack: 20);
        var enemy = CreateEnemy(speed: 5, defense: 0, health: 10);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var result = combat.SubmitAction(
            CombatAction.BasicAttack(player.Id, enemy.Id));

        result.TargetDefeated.Should().BeTrue();
        result.CombatState.Should().Be(CombatState.Completed);
        result.WinningSide.Should().Be(CombatantSide.Player);
        result.NextActorId.Should().BeNull();
    }

    [Fact]
    public void SubmitAction_ShouldThrow_WhenItIsNotActorTurn()
    {
        var player = CreatePlayer(speed: 10);
        var enemy = CreateEnemy(speed: 5);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var act = () => combat.SubmitAction(
            CombatAction.BasicAttack(enemy.Id, player.Id));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("It is not this combatant's turn.");
    }

    [Fact]
    public void SubmitAction_ShouldThrow_WhenTargetingAlly()
    {
        var playerOne = CreatePlayer(speed: 10, templateKey: "player-1");
        var playerTwo = CreatePlayer(speed: 5, templateKey: "player-2");
        var enemy = CreateEnemy(speed: 1);

        var combat = CombatInstance.Create(new[] { playerOne, playerTwo, enemy });

        var act = () => combat.SubmitAction(
            CombatAction.BasicAttack(playerOne.Id, playerTwo.Id));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Combatant cannot target an ally.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoEnemyCombatantExists()
    {
        var playerOne = CreatePlayer(speed: 10, templateKey: "player-1");
        var playerTwo = CreatePlayer(speed: 5, templateKey: "player-2");

        var act = () => CombatInstance.Create(new[] { playerOne, playerTwo });

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Combat requires at least one enemy-side combatant.");
    }

    private static CombatantSnapshot CreatePlayer(
        int speed,
        int attack = 12,
        int defense = 2,
        int health = 40,
        string templateKey = "player-runtime-v1")
    {
        return CombatantSnapshot.Create(
            templateKey,
            displayName: "Player",
            CombatantSide.Player,
            maxHealth: health,
            attack,
            defense,
            speed);
    }

    private static CombatantSnapshot CreateEnemy(
        int speed,
        int attack = 8,
        int defense = 2,
        int health = 30,
        string templateKey = "enemy-shadow-v1")
    {
        return CombatantSnapshot.Create(
            templateKey,
            displayName: "Shadow",
            CombatantSide.Enemy,
            maxHealth: health,
            attack,
            defense,
            speed);
    }
}