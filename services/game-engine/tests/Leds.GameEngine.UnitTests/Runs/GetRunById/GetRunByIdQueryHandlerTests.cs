using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetRunById;

public sealed class GetRunByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRun_WhenRunExists()
    {
        var run = CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new GetRunByIdQueryHandler(repository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.PlayerId.Should().Be(run.PlayerId);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        var allNodes = response.Run.CurrentRoom.NodeLayers
    .SelectMany(layer => layer.Nodes)
    .ToArray();

        allNodes.Should().HaveCount(response.Run.CurrentRoom.TotalNodeCount);
        allNodes.Should().HaveCountGreaterThanOrEqualTo(6);
        allNodes.Should().HaveCountLessThanOrEqualTo(10);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .HaveCountGreaterThanOrEqualTo(1)
            .And
            .HaveCountLessThanOrEqualTo(4);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available" && node.NodeDepth == 0);

        allNodes
            .Where(node => node.NodeDepth > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes
            .Should()
            .ContainSingle(node => node.IsRoomBossNode);

        allNodes
            .Should()
            .OnlyContain(node => node.EventCount >= 1 && node.EventCount <= 4);

        allNodes
            .Should()
            .OnlyContain(node => node.EventTypes.Count >= 1 && node.EventTypes.Count <= 4); ;
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var handler = new GetRunByIdQueryHandler(repository.Object);

        var act = () => handler.Handle(
            new GetRunByIdQuery(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
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