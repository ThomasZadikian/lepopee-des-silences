using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTacticalExplorationTests
{
    [Fact]
    public void StartNew_ShouldDefaultExplorationMode_ToClassic()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ExplorationMode.Should().Be(RunExplorationMode.Classic);
    }

    [Fact]
    public void StartNew_ShouldHonorExplorationMode_WhenTacticalRoomIsProvided()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        run.ExplorationMode.Should().Be(RunExplorationMode.Tactical);
        run.CurrentRoom.Grid.Should().NotBeNull();
    }

    [Fact]
    public void MoveParty_ShouldDelegateToCurrentRoom()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        run.MoveParty(1, 0);

        run.CurrentRoom.Grid!.PartyX.Should().Be(1);
        run.CurrentRoom.Grid.PartyY.Should().Be(0);
    }

    [Fact]
    public void MoveParty_ShouldThrow_OnAClassicRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.MoveParty(1, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EnterGridNode_ShouldSelectTheNode_WhenPartyIsOnItsCell()
    {
        var run = TestGameEngineFactory.CreateGridRun();
        var node = run.CurrentRoom.Nodes.First(n => !n.IsBoss);

        run.MoveParty(node.Lane, node.Row);
        run.EnterGridNode(node.Id.Value);

        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void ChallengeBossRemotely_ShouldSelectTheBoss_WhenBudgetIsExhausted()
    {
        var room = TestGameEngineFactory.CreateGridThresholdRoom(movementBudget: 8).Room;
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-grid-remote-boss",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            explorationMode: RunExplorationMode.Tactical);
        var boss = run.CurrentRoom.Nodes.Single(n => n.IsBoss);

        run.MoveParty(4, 0); // costs 4, remaining 4
        run.MoveParty(0, 0); // costs 4 back to start, remaining 0

        run.ChallengeBossRemotely();

        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
        boss.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldResolveTheGridNode_AndKeepRunActive_ForANonBossNode()
    {
        var run = TestGameEngineFactory.CreateGridRun();
        var node = run.CurrentRoom.Nodes.First(n => !n.IsBoss);
        run.MoveParty(node.Lane, node.Row);
        run.EnterGridNode(node.Id.Value);

        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
        run.Status.Should().Be(RunStatus.Active);
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldCompleteTheRoom_WhenTheGridBossIsResolved()
    {
        var run = TestGameEngineFactory.CreateGridRun();
        var boss = run.CurrentRoom.Nodes.Single(n => n.IsBoss);
        run.MoveParty(boss.Lane, boss.Row);
        run.EnterGridNode(boss.Id.Value);

        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.Completed);
        run.Status.Should().Be(RunStatus.RoomResolved);
    }

    [Fact]
    public void ProgressCurrentRoom_ShouldReturnToGridExploration()
    {
        var run = TestGameEngineFactory.CreateGridRun();
        var node = run.CurrentRoom.Nodes.First(n => !n.IsBoss);
        run.MoveParty(node.Lane, node.Row);
        run.EnterGridNode(node.Id.Value);
        run.ResolveCurrentEvent();

        run.ProgressCurrentRoom();

        run.CurrentRoom.State.Should().Be(RoomState.Active);
    }

    // -----------------------------------------------------------------------
    // Non-regression: Classic-mode behavior at the shared "point de couture"
    // call sites must remain byte-for-byte identical.
    // -----------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldStillResolveTheClassicNode_ByRowLookup()
    {
        var run = TestGameEngineFactory.CreateRun();
        var node = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(node.Id);

        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void ProgressCurrentRoom_ShouldStillUnlockTheNextClassicLayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        var node = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(node.Id);
        run.ResolveCurrentEvent();

        run.ProgressCurrentRoom();

        run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
    }
}
