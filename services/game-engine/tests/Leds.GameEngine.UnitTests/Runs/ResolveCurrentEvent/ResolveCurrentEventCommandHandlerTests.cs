using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandlerTests
{
    private static Mock<INodeEventResolverDispatcher> CreateDispatcherMock(
    NodeEventResolutionKind resolutionKind = NodeEventResolutionKind.CombatStarted)
    {
        var dispatcher = new Mock<INodeEventResolverDispatcher>();

        dispatcher
            .Setup(service => service.Resolve(It.IsAny<NodeEventResolutionContext>()))
            .Returns(NodeEventResolutionResult.Create(
                resolutionKind,
                "Test outcome",
                "Test outcome description",
                narrativeFragments: new[]
                {
                new NarrativeFragmentDto(
                    "Elise",
                    "Test narrative fragment.")
                }));

        return dispatcher;
    }
    [Fact]
    public async Task Handle_ShouldResolveCurrentEvent_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object);

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());

        var resolvedNode = response.Run.CurrentRoom.NodeLayers.SelectMany(layer => layer.Nodes)
            .Single(node => node.Id == selectedNode.Id.Value);

        resolvedNode.State.Should().Be(NodeState.Resolved.ToString());

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenNoNodeWasSelected()
    {
        var run = CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("No node has been selected for the current room depth.");
    }

    private static Run CreateRun()
    {
        return Run.StartNew(
            Guid.NewGuid(),
            "seed-test-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);
    }

    private static Room CreateInitialRoom()
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");

        var combatNode = Node.Create(
            NodeEventType.Combat,
            20,
            "combat-common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var itemNode = Node.Create(
            NodeEventType.Item,
            15,
            "common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var restNode = Node.Create(
            NodeEventType.Rest,
            5,
            "healing-only",
            nodeDepth: 1,
            parentNodeId: combatNode.Id,
            initialState: NodeState.Planned);

        var npcNode = Node.Create(
                new[]
                {
                    NodeEvent.Create(NodeEventType.Npc, 1)
                },
            8,
            "narrative-choice",
            nodeDepth: 1,
            parentNodeIds: new[]
            {
            combatNode.Id,
            itemNode.Id
            },
            isRoomBossNode: false,
            initialState: NodeState.Planned);

        var rareNode = Node.Create(
            NodeEventType.Rare,
            25,
            "rare",
            nodeDepth: 1,
            parentNodeId: itemNode.Id,
            initialState: NodeState.Planned);

        var bossNode = Node.Create(
            new[]
            {
            NodeEvent.Create(NodeEventType.RoomBoss, 1)
            },
            riskLevel: 80,
            rewardProfile: "room-boss",
            nodeDepth: 2,
            parentNodeIds: new[]
            {
            restNode.Id,
            npcNode.Id,
            rareNode.Id
            },
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        return Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            new[]
            {
            combatNode,
            itemNode,
            restNode,
            npcNode,
            rareNode,
            bossNode
            });
    }
}