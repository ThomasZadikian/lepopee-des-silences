using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalMovementTests
{
    [Fact]
    public void StepCost_ShouldApplyCanonicalElevationCosts()
    {
        var battlefield = Battlefield(
            width: 3,
            height: 1,
            elevation: [0, 1, 0]);

        battlefield.StepCost(new GridPosition(0, 0), new GridPosition(1, 0))
            .Should().Be(2);
        battlefield.StepCost(new GridPosition(1, 0), new GridPosition(2, 0))
            .Should().Be(0);
    }

    [Fact]
    public void ReachableCells_ShouldAllowPassingThroughAllyButNotStoppingOnIt()
    {
        var battlefield = Battlefield(width: 4, height: 1, elevation: [0, 0, 0, 0]);
        var allyCell = new GridPosition(1, 0);
        IReadOnlySet<GridPosition> occupied = new HashSet<GridPosition> { allyCell };
        IReadOnlySet<GridPosition> traversableAllies = new HashSet<GridPosition> { allyCell };

        var reachable = TacticalMovement.ReachableCells(
            battlefield,
            new GridPosition(0, 0),
            budget: 3,
            occupied,
            traversableAllies);

        reachable.Should().NotContainKey(allyCell);
        reachable.Should().ContainKey(new GridPosition(2, 0));
        reachable.Should().ContainKey(new GridPosition(3, 0));
    }

    [Fact]
    public void ReachableCells_ShouldNotAllowPassingThroughEnemy()
    {
        var battlefield = Battlefield(width: 3, height: 1, elevation: [0, 0, 0]);
        IReadOnlySet<GridPosition> occupied =
            new HashSet<GridPosition> { new GridPosition(1, 0) };

        var reachable = TacticalMovement.ReachableCells(
            battlefield,
            new GridPosition(0, 0),
            budget: 4,
            occupied);

        reachable.Should().NotContainKey(new GridPosition(2, 0));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    public void BudgetFor_ShouldEnforceMinimumOne(int movement, int expected)
    {
        TacticalMovement.BudgetFor(movement).Should().Be(expected);
    }

    [Fact]
    public void FindInterceptor_ShouldReturnFirstCombatantOnTheLine()
    {
        var battlefield = Battlefield(width: 4, height: 1, elevation: [0, 0, 0, 0]);
        var actor = Combatant.CreateAlly("player.actor", "Acteur", "Porteur", 40);
        var blocker = Combatant.CreateAlly("player.blocker", "Intercepteur", "Porteur", 40);
        var target = Combatant.CreateEnemy("enemy.target", "Cible", "Bruiser", 40);
        var combat = Combat(
            battlefield,
            [(actor, new GridPosition(0, 0)), (blocker, new GridPosition(1, 0))],
            [(target, new GridPosition(3, 0))]);

        TacticalTargeting.FindInterceptor(
                combat,
                combat.PositionOf(actor.Id.Value),
                combat.PositionOf(target.Id.Value))
            .Should().BeSameAs(blocker);
    }

    [Fact]
    public void FindInterceptor_ShouldBeBypassedByPlungingSight()
    {
        var battlefield = Battlefield(width: 3, height: 1, elevation: [1, 0, 0]);
        var actor = Combatant.CreateAlly("player.actor", "Acteur", "Porteur", 40);
        var blocker = Combatant.CreateAlly("player.blocker", "Intercepteur", "Porteur", 40);
        var target = Combatant.CreateEnemy("enemy.target", "Cible", "Bruiser", 40);
        var combat = Combat(
            battlefield,
            [(actor, new GridPosition(0, 0)), (blocker, new GridPosition(1, 0))],
            [(target, new GridPosition(2, 0))]);

        TacticalTargeting.FindInterceptor(
                combat,
                combat.PositionOf(actor.Id.Value),
                combat.PositionOf(target.Id.Value))
            .Should().BeNull();
    }

    [Theory]
    [InlineData(0, 0, TacticalAttackArc.Face)]
    [InlineData(0, 1, TacticalAttackArc.Flank)]
    [InlineData(0, 2, TacticalAttackArc.Back)]
    public void AttackArcOf_ShouldClassifyFrontAndRearDiagonals(
        int attackerX,
        int attackerY,
        TacticalAttackArc expected)
    {
        var battlefield = Battlefield(
            width: 3,
            height: 3,
            elevation: Enumerable.Repeat(0, 9).ToArray());
        var actor = Combatant.CreateAlly("player.actor", "Acteur", "Porteur", 40);
        var target = Combatant.CreateEnemy("enemy.target", "Cible", "Bruiser", 40);
        var combat = Combat(
            battlefield,
            [(actor, new GridPosition(attackerX, attackerY))],
            [(target, new GridPosition(1, 1))]);
        combat.OrientToward(target.Id.Value, new GridPosition(1, 0));

        combat.AttackArcOf(actor.Id.Value, target.Id.Value).Should().Be(expected);
    }

    [Fact]
    public void ForceMove_ShouldApplyCollisionToPushedUnitAndHalfToOccupant()
    {
        var battlefield = Battlefield(width: 3, height: 1, elevation: [0, 0, 0]);
        var pushed = Combatant.Create(
            CombatantId.New(), "player.pushed", "Poussé", CombatantSide.Player, "Porteur",
            100, 100, 10, 10, 0, 0);
        var occupant = Combatant.CreateEnemy("enemy.blocker", "Obstacle", "Bruiser", 100);
        var combat = Combat(
            battlefield,
            [(pushed, new GridPosition(0, 0))],
            [(occupant, new GridPosition(1, 0))]);

        var result = combat.ForceMove(pushed.Id.Value, 1, 0, distance: 3);

        result.CollisionDamage.Should().Be(15);
        pushed.Guard.Should().Be(0);
        pushed.CurrentVitality.Should().Be(95);
        occupant.CurrentVitality.Should().Be(92);
    }

    [Fact]
    public void ForceMove_ShouldApplyFivePercentPerDescendedLevel()
    {
        var battlefield = Battlefield(width: 2, height: 1, elevation: [2, 0]);
        var pushed = Combatant.CreateAlly("player.pushed", "Poussé", "Porteur", 100);
        var enemy = Combatant.CreateEnemy("enemy.test", "Ennemi", "Bruiser", 100);
        var combat = Combat(
            battlefield,
            [(pushed, new GridPosition(0, 0))],
            [(enemy, new GridPosition(1, 0))]);
        enemy.MarkDefeated();

        var result = combat.ForceMove(pushed.Id.Value, 1, 0, distance: 1);

        result.FallDamage.Should().Be(10);
        pushed.CurrentVitality.Should().Be(90);
    }

    [Fact]
    public void ForceMove_ShouldImmediatelyEliminateUnitPushedIntoVoid()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            3,
            1,
            [0, 0, 0],
            [true, true, false],
            [true, true, false]);
        var pushed = Combatant.CreateAlly("player.pushed", "Poussé", "Porteur", 100);
        var enemy = Combatant.CreateEnemy("enemy.test", "Ennemi", "Bruiser", 100);
        var combat = Combat(
            battlefield,
            [(pushed, new GridPosition(1, 0))],
            [(enemy, new GridPosition(0, 0))]);

        var result = combat.ForceMove(pushed.Id.Value, 1, 0, distance: 1);

        result.EliminatedByVoid.Should().BeTrue();
        pushed.IsDefeated.Should().BeTrue();
    }

    private static TacticalBattlefield Battlefield(
        int width,
        int height,
        IReadOnlyList<int> elevation) =>
        TacticalBattlefield.Rehydrate(
            width,
            height,
            elevation,
            Enumerable.Repeat(true, width * height).ToArray(),
            Enumerable.Repeat(true, width * height).ToArray());

    private static TacticalCombat Combat(
        TacticalBattlefield battlefield,
        IReadOnlyCollection<(Combatant Combatant, GridPosition Position)> allies,
        IReadOnlyCollection<(Combatant Combatant, GridPosition Position)> enemies) =>
        TacticalCombat.Create(
            CombatId.New(),
            new RunId(Guid.NewGuid()),
            new RoomId(Guid.NewGuid()),
            new NodeId(Guid.NewGuid()),
            battlefield,
            allies,
            enemies,
            DateTime.UtcNow);
}
