using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public void GenerateInitialRoom_ShouldCreateVisibleRoomPlan_WithSixToTenNodes()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Depth.Should().Be(0);
        room.RoomType.Should().Be(RoomType.Threshold);
        room.Theme.Should().Be("Threshold");
        room.State.Should().Be(RoomState.Active);
        room.CurrentNodeDepth.Should().Be(0);

        room.TotalNodeCount.Should().BeInRange(6, 10);
        room.Nodes.Should().HaveCount(room.TotalNodeCount);

        room.AvailableNodes.Should().HaveCountGreaterThanOrEqualTo(1);
        room.AvailableNodes.Should().HaveCountLessThanOrEqualTo(4);
        room.AvailableNodes.Should().OnlyContain(node => node.NodeDepth == 0);
        room.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);

        room.Nodes
            .Where(node => node.NodeDepth > 0)
            .Should()
            .OnlyContain(node => node.State == NodeState.Planned);

        room.Nodes.Should().ContainSingle(node => node.IsRoomBossNode);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateNodes_WithOneToFourEventsEach()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Nodes.Should().OnlyContain(node =>
            node.EventCount >= 1 && node.EventCount <= 4);

        room.Nodes.Should().OnlyContain(node =>
            node.EventTypes.Count >= 1 && node.EventTypes.Count <= 4);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateRoomBossMatchingRoomType()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.BossProfile.Should().NotBeNull();
        room.BossProfile.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.BossId.Should().Be("threshold-guardian");
        room.BossProfile.Name.Should().Be("Gardien du Seuil");
        room.BossProfile.DangerHint.Should().Be("High");

        var bossNode = room.Nodes.Single(node => node.IsRoomBossNode);

        bossNode.NodeDepth.Should().Be(room.MaxNodeDepth);
        bossNode.State.Should().Be(NodeState.Planned);
        bossNode.EventTypes.Should().Contain(NodeEventType.RoomBoss);
        bossNode.EventCount.Should().Be(1);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateLayers_WithOneToFourNodesEach()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var layers = room.Nodes
            .GroupBy(node => node.NodeDepth)
            .ToArray();

        layers.Should().NotBeEmpty();

        layers.Should().OnlyContain(layer =>
            layer.Count() >= 1 && layer.Count() <= 4);

        layers.Select(layer => layer.Key)
            .Should()
            .Contain(0);

        layers.Select(layer => layer.Key)
            .Should()
            .Contain(room.MaxNodeDepth);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldBeDeterministic_ForVisibleRoomPlan()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var firstRoom = generator.GenerateInitialRoom("seed-test-001");
        var secondRoom = generator.GenerateInitialRoom("seed-test-001");

        var firstSnapshot = CreateRoomPlanSnapshot(firstRoom);
        var secondSnapshot = CreateRoomPlanSnapshot(secondRoom);

        secondSnapshot.Should().BeEquivalentTo(
            firstSnapshot,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void GenerateInitialRoom_ShouldGenerateDifferentPlans_ForDifferentSeeds()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var firstRoom = generator.GenerateInitialRoom("seed-test-001");
        var secondRoom = generator.GenerateInitialRoom("seed-test-002");

        var firstSnapshot = CreateRoomPlanSnapshot(firstRoom);
        var secondSnapshot = CreateRoomPlanSnapshot(secondRoom);

        secondSnapshot.Should().NotBeEquivalentTo(firstSnapshot);
    }

    private static object[] CreateRoomPlanSnapshot(Room room)
    {
        return room.Nodes
            .OrderBy(node => node.NodeDepth)
            .ThenBy(node => node.ParentNodeId?.Value)
            .ThenBy(node => node.RiskLevel)
            .Select(node => new
            {
                node.NodeDepth,
                ParentNodeIdIsPresent = node.ParentNodeId is not null,
                node.IsRoomBossNode,
                node.RiskLevel,
                node.RewardProfile,
                EventTypes = node.EventTypes
                    .Select(eventType => eventType.ToString())
                    .ToArray(),
                node.EventCount,
                InitialState = node.State.ToString()
            })
            .Cast<object>()
            .ToArray();
    }
    [Fact]
    public void GenerateInitialRoom_ShouldCreateConvergentGraph_ToRoomBoss()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var bossNode = room.Nodes.Single(node => node.IsRoomBossNode);

        foreach (var node in room.Nodes.Where(node => !node.IsRoomBossNode))
        {
            HasPathToBoss(node, bossNode, room.Nodes).Should().BeTrue();
        }
    }

    private static bool HasPathToBoss(
        Node currentNode,
        Node bossNode,
        IReadOnlyCollection<Node> nodes)
    {
        var children = nodes
            .Where(node => node.ParentNodeIds.Contains(currentNode.Id))
            .ToArray();

        if (children.Any(child => child.Id == bossNode.Id))
        {
            return true;
        }

        return children.Any(child => HasPathToBoss(child, bossNode, nodes));
    }
    [Fact]
    public void GenerateInitialRoom_ShouldGiveEveryNonBossNodeAtLeastOneChild()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        foreach (var node in room.Nodes.Where(node => !node.IsRoomBossNode))
        {
            room.Nodes
                .Any(candidate => candidate.ParentNodeIds.Contains(node.Id))
                .Should()
                .BeTrue();
        }
    }

    [Fact]
    public void GenerateInitialRoom_ShouldPlaceSingleRoomBossNodeAtFinalDepth()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var finalLayerNodes = room.Nodes
            .Where(node => node.NodeDepth == room.MaxNodeDepth)
            .ToArray();

        finalLayerNodes.Should().ContainSingle();

        var bossNode = finalLayerNodes.Single();

        bossNode.IsRoomBossNode.Should().BeTrue();
        bossNode.EventTypes.Should().Contain(NodeEventType.RoomBoss);
        bossNode.State.Should().Be(NodeState.Planned);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldRespectNodeEventTypeRoomConstraints()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var allEventTypes = room.Nodes
            .SelectMany(node => node.EventTypes)
            .ToArray();

        allEventTypes.Should().NotContain(NodeEventType.Memory);
        allEventTypes.Count(eventType => eventType == NodeEventType.Elite).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Item).Should().BeLessThanOrEqualTo(room.TotalNodeCount / 2);
        allEventTypes.Count(eventType => eventType == NodeEventType.Npc).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Rest).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Merchant).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Law).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Curse).Should().BeLessThanOrEqualTo(1);
        allEventTypes.Count(eventType => eventType == NodeEventType.Rare).Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldRespectNodeEventTypeNodeConstraints()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        foreach (var node in room.Nodes.Where(node => !node.IsRoomBossNode))
        {
            node.EventTypes.Count(eventType => eventType == NodeEventType.Item)
                .Should()
                .BeLessThanOrEqualTo(1);

            if (node.EventTypes.Contains(NodeEventType.Rest))
            {
                node.EventTypes.Should().ContainSingle();
                node.RewardProfile.Should().Be("healing-only");
            }
        }
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateAtLeastTwoAvailableNodesAtInitialDepth()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.AvailableNodes.Should().HaveCountGreaterThanOrEqualTo(2);
        room.AvailableNodes.Should().HaveCountLessThanOrEqualTo(4);
        room.AvailableNodes.Should().OnlyContain(node => node.NodeDepth == 0);
        room.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }
}