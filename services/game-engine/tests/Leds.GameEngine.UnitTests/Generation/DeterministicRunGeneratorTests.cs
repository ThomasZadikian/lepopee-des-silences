using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public void GenerateInitialRoom_ShouldCreateRoom_WithFourInitialNodes()
    {
        var generator = new DeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Depth.Should().Be(0);
        room.Theme.Should().Be("Threshold");
        room.Nodes.Should().HaveCount(4);
        room.Nodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldGenerateExpectedInitialEventTypes()
    {
        var generator = new DeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Nodes.Select(node => node.EventType)
            .Should()
            .BeEquivalentTo(new[]
            {
                NodeEventType.Combat,
                NodeEventType.Memory,
                NodeEventType.Rest,
                NodeEventType.Item
            });
    }

    [Fact]
    public void GenerateInitialRoom_ShouldBeDeterministic_ForRiskAndRewardProfiles()
    {
        var generator = new DeterministicRunGenerator();

        var firstRoom = generator.GenerateInitialRoom("seed-test-001");
        var secondRoom = generator.GenerateInitialRoom("seed-test-001");

        var firstSnapshot = firstRoom.Nodes
            .Select(node => new
            {
                node.EventType,
                node.RiskLevel,
                node.RewardProfile
            })
            .ToArray();

        var secondSnapshot = secondRoom.Nodes
            .Select(node => new
            {
                node.EventType,
                node.RiskLevel,
                node.RewardProfile
            })
            .ToArray();

        secondSnapshot.Should().BeEquivalentTo(firstSnapshot, options => options.WithStrictOrdering());
    }
    [Fact]
    public void GenerateNextNodes_ShouldCreateNextNodeLayer_WhenCurrentEventIsResolved()
    {
        var generator = new DeterministicRunGenerator();
        var run = CreateRunWithResolvedCurrentEvent(generator);

        var nextNodes = generator.GenerateNextNodes(run);

        nextNodes.Should().NotBeEmpty();
        nextNodes.Count.Should().BeInRange(1, 4);
        nextNodes.Should().OnlyContain(node => node.NodeDepth == 1);
        nextNodes.Should().OnlyContain(node => node.ParentNodeId != null);
        nextNodes.Should().OnlyContain(node => node.State == NodeState.Available);
        nextNodes.Should().OnlyContain(node => node.IsRoomBossNode == false);
    }

    [Fact]
    public void GenerateNextNodes_ShouldCreateRoomBossNode_WhenNextDepthIsMaxNodeDepth()
    {
        var generator = new DeterministicRunGenerator();

        var room = Room.Create(
            0,
            "Threshold",
            new[]
            {
            Node.Create(NodeEventType.Combat, 20, "common"),
            Node.Create(NodeEventType.Memory, 10, "common"),
            Node.Create(NodeEventType.Rest, 5, "none"),
            Node.Create(NodeEventType.Item, 15, "common")
            },
            maxNodeDepth: 1);

        var run = Domain.Runs.Run.StartNew(
            Guid.NewGuid(),
            "seed-test-001",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            room,
            DateTimeOffset.UtcNow);

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        var nextNodes = generator.GenerateNextNodes(run);

        nextNodes.Should().ContainSingle();

        var bossNode = nextNodes.Single();

        bossNode.EventType.Should().Be(NodeEventType.RoomBoss);
        bossNode.NodeDepth.Should().Be(1);
        bossNode.IsRoomBossNode.Should().BeTrue();
        bossNode.ParentNodeId.Should().Be(selectedNode.Id);
        bossNode.State.Should().Be(NodeState.Available);
    }

    private static Domain.Runs.Run CreateRunWithResolvedCurrentEvent(
        DeterministicRunGenerator generator)
    {
        var initialRoom = generator.GenerateInitialRoom("seed-test-001");

        var run = Domain.Runs.Run.StartNew(
            Guid.NewGuid(),
            "seed-test-001",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow);

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        return run;
    }
}