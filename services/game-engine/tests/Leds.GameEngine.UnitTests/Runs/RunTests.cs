using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTests
{
    [Fact]
    public void StartNew_ShouldCreateActiveRun_WithInitialRoomAndMetadata()
    {
        var playerId = Guid.NewGuid();
        var initialRoom = CreateInitialRoom();

        var run = Run.StartNew(
            playerId,
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            initialRoom,
            DateTimeOffset.UtcNow);

        run.Id.Value.Should().NotBeEmpty();
        run.PlayerId.Should().Be(playerId);
        run.Seed.Should().Be("seed-001");
        run.GeneratorVersion.Should().Be("gen-0.1.0");
        run.MarkovMatrixVersion.Should().Be("markov-0.1.0");
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentDepth.Should().Be(0);
        run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        run.CurrentRoom.MaxNodeDepth.Should().Be(3);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.AvailableNodes.Should().HaveCount(4);
    }

    [Fact]
    public void StartNew_ShouldThrow_WhenInitialRoomDoesNotHaveExactlyFourAvailableNodes()
    {
        var room = Room.Create(
            0,
            "Threshold",
            new[]
            {
                Node.Create(NodeEventType.Combat, 20, "common"),
                Node.Create(NodeEventType.Memory, 10, "common")
            });

        var act = () => Run.StartNew(
            Guid.NewGuid(),
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            room,
            DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("A new run must start with exactly 4 available nodes.");
    }

    [Fact]
    public void ChooseNode_ShouldSelectRequestedNode_AndLockOtherNodesAtCurrentDepth()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        selectedNode.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);

        run.CurrentRoom.Nodes
            .Where(node => node.NodeDepth == run.CurrentRoom.CurrentNodeDepth &&
                           node.Id != selectedNode.Id)
            .Should()
            .OnlyContain(node => node.State == NodeState.Locked);
    }

    [Fact]
    public void ChooseNode_ShouldThrow_WhenNodeWasAlreadySelectedAtCurrentDepth()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);

        var firstNode = run.CurrentRoom.AvailableNodes.First();
        var secondNode = run.CurrentRoom.AvailableNodes.Last();

        run.ChooseNode(firstNode.Id);

        var act = () => run.ChooseNode(secondNode.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room is not waiting for a node selection.");
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldResolveSelectedNode_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        selectedNode.State.Should().Be(NodeState.Resolved);
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public void Abandon_ShouldCloseRun()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.Abandon(endedAt);

        run.Status.Should().Be(RunStatus.Abandoned);
        run.EndedAt.Should().Be(endedAt);
    }

    private static Room CreateInitialRoom()
    {
        return Room.Create(
            0,
            "Threshold",
            new[]
            {
                Node.Create(NodeEventType.Combat, 20, "common"),
                Node.Create(NodeEventType.Memory, 10, "common"),
                Node.Create(NodeEventType.Rest, 5, "none"),
                Node.Create(NodeEventType.Item, 15, "common")
            },
            maxNodeDepth: 3);
    }
}