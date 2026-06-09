using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCombatCompletionTests
{
    [Fact]
    public void CompleteActiveCombat_ShouldClearActiveCombat_WhenCombatIsCompleted()
    {
        var (run, combat) = CreateRunWithSelectedNodeAndCombat();
        combat.MarkCompleted();

        run.CompleteActiveCombat();

        run.ActiveCombat.Should().BeNull();
        run.HasActiveCombat.Should().BeFalse();
    }

    [Fact]
    public void CompleteActiveCombat_ShouldThrow_WhenNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;

        var act = () => run.CompleteActiveCombat();

        act.Should().Throw<DomainException>().WithMessage("Run has no active combat.");
    }

    [Fact]
    public void CompleteActiveCombat_ShouldThrow_WhenCombatIsStillActive()
    {
        var (run, _) = CreateRunWithSelectedNodeAndCombat();

        var act = () => run.CompleteActiveCombat();

        act.Should().Throw<DomainException>()
            .WithMessage("Active combat must be completed before resolving combat victory.");
    }

    [Fact]
    public void CompleteActiveCombat_ShouldAllowRunProgression()
    {
        var (run, combat) = CreateRunWithSelectedNodeAndCombat();
        combat.MarkCompleted();

        run.CompleteActiveCombat();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
        run.ProgressCurrentRoom();
        run.CurrentRoom.State.Should().Be(RoomState.Active);
    }

    [Fact]
    public void FailActiveCombat_ShouldSetRunFailed_WhenCombatFailed()
    {
        var (run, combat) = CreateRunWithSelectedNodeAndCombat();
        combat.MarkFailed();

        run.FailActiveCombat(DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Failed);
        run.ActiveCombat.Should().BeNull();
        run.HasActiveCombat.Should().BeFalse();
    }

    [Fact]
    public void FailActiveCombat_ShouldThrow_WhenNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;

        var act = () => run.FailActiveCombat(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Run has no active combat.");
    }

    [Fact]
    public void FailActiveCombat_ShouldThrow_WhenCombatIsNotFailed()
    {
        var (run, _) = CreateRunWithSelectedNodeAndCombat();

        var act = () => run.FailActiveCombat(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("Active combat must be failed before resolving combat defeat.");
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenPreviousActiveCombatIsNotFinalized()
    {
        var (run, _) = CreateRunWithSelectedNodeAndCombat();
        var anotherCombat = CreateCombat(run.Id);

        var act = () => run.StartCombat(anotherCombat);

        act.Should().Throw<DomainException>().WithMessage("Run already has an active combat.");
    }

    private static (Run Run, Combat Combat) CreateRunWithSelectedNodeAndCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var combat = CreateCombat(run.Id);
        run.StartCombat(combat);

        return (run, combat);
    }

    private static Combat CreateCombat(RunId runId)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        return Combat.Create(
            CombatId.New(),
            runId,
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);
    }
}
