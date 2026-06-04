using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class StartRunCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateRun_AndPersistIt()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = CreateInitialRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-001");
        generator.Setup(service => service.GenerateInitialRoom("seed-test-001")).Returns(initialRoom);

        var repository = new Mock<IRunRepository>();

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            clock.Object);

        var response = await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        response.Run.Id.Should().NotBeEmpty();
        response.Run.PlayerId.Should().Be(playerId);
        response.Run.Seed.Should().Be("seed-test-001");
        response.Run.GeneratorVersion.Should().Be("gen-0.1.0");
        response.Run.MarkovMatrixVersion.Should().Be("markov-0.1.0");
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
            .OnlyContain(node => node.EventTypes.Count >= 1 && node.EventTypes.Count <= 4);

        repository.Verify(
            repo => repo.AddAsync(
                It.Is<Run>(run =>
                    run.PlayerId == playerId &&
                    run.Seed == "seed-test-001" &&
                    run.Status == RunStatus.Active),
                CancellationToken.None),
            Times.Once);
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