using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCombatCompletionTests
{
    [Fact]
    public void CompleteActiveCombat_ShouldClearActiveCombatId()
    {
        var (run, combat) = CreateRunWithActiveCombatAndSelectedNode();

        run.CompleteActiveCombat(combat.Id);

        run.HasActiveCombat.Should().BeFalse();
        run.ActiveCombatId.Should().BeNull();
    }

    [Fact]
    public void CompleteActiveCombat_ShouldResolveCurrentEvent()
    {
        var (run, combat) = CreateRunWithActiveCombatAndSelectedNode();

        run.CompleteActiveCombat(combat.Id);

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public void CompleteActiveCombat_ShouldThrow_WhenNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.CompleteActiveCombat(CombatId.New());

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run has no active combat.");
    }

    [Fact]
    public void CompleteActiveCombat_ShouldThrow_WhenCombatIdDoesNotMatch()
    {
        var (run, _) = CreateRunWithActiveCombatAndSelectedNode();
        var otherCombatId = CombatId.New();

        var act = () => run.CompleteActiveCombat(otherCombatId);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Combat does not match the active run combat.");
    }

    [Fact]
    public void FailActiveCombat_ShouldSetRunToFailed()
    {
        var (run, combat) = CreateRunWithActiveCombatAndSelectedNode();

        run.FailActiveCombat(combat.Id, DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Failed);
        run.HasActiveCombat.Should().BeFalse();
        run.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public void FailActiveCombat_ShouldThrow_WhenNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.FailActiveCombat(CombatId.New(), DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run has no active combat.");
    }

    [Fact]
    public void FailActiveCombat_ShouldThrow_WhenCombatIdDoesNotMatch()
    {
        var (run, _) = CreateRunWithActiveCombatAndSelectedNode();
        var otherCombatId = CombatId.New();

        var act = () => run.FailActiveCombat(otherCombatId, DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Combat does not match the active run combat.");
    }

    private static (Run run, CombatInstance combat) CreateRunWithActiveCombatAndSelectedNode()
    {
        var (run, combat) = CreateRunWithActiveCombat();
        run.ChooseNode(run.CurrentRoom.AvailableNodes.First().Id);
        return (run, combat);
    }

    private static (Run run, CombatInstance combat) CreateRunWithActiveCombat()
    {
        var player = CombatantSnapshot.Create(
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: 40,
            attack: 12,
            defense: 6,
            speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-shadow-v1",
            "Shadow Enemy",
            CombatantSide.Enemy,
            maxHealth: 30,
            attack: 8,
            defense: 4,
            speed: 6);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var run = TestGameEngineFactory.CreateRun();
        run.SetActiveCombat(combat.Id);

        return (run, combat);
    }
}
