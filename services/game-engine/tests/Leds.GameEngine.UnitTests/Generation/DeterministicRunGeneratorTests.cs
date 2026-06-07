using FluentAssertions;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public void GenerateInitialRoom_ShouldCreateVisibleRoomPlan()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Depth.Should().Be(0);
        room.RoomType.Should().Be(RoomType.Threshold);
        room.Theme.Should().Be("Threshold");
        room.State.Should().Be(RoomState.Active);
        room.CurrentNodeDepth.Should().Be(0);

        room.TotalNodeCount.Should().Be(22);
        room.Nodes.Should().HaveCount(room.TotalNodeCount);

        room.AvailableNodes.Should().HaveCount(2);
        room.AvailableNodes.Should().OnlyContain(node => node.Row == 0);
        room.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);

        room.Nodes
            .Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node => node.State == NodeState.Planned);

        room.Nodes.Should().ContainSingle(node => node.IsBoss);
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

        var bossNode = room.Nodes.Single(node => node.IsBoss);

        bossNode.Row.Should().Be(room.MaxNodeDepth);
        bossNode.State.Should().Be(NodeState.Planned);
        bossNode.EventType.Should().Be(NodeEventType.RoomBoss);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateEightRows()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var rowCount = room.Nodes
            .Select(node => node.Row)
            .Distinct()
            .Count();

        rowCount.Should().Be(8);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldBeDeterministic()
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
            .OrderBy(node => node.Row)
            .ThenBy(node => node.Lane)
            .ThenBy(node => node.RiskLevel)
            .Select(node => new
            {
                node.Row,
                node.Lane,
                node.IsBoss,
                node.RiskLevel,
                node.RewardProfile,
                EventType = node.EventType.ToString(),
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

        var bossNode = room.Nodes.Single(node => node.IsBoss);

        foreach (var node in room.Nodes.Where(node => !node.IsBoss))
        {
            HasPathToBoss(node, bossNode, room.Nodes).Should().BeTrue();
        }
    }

    private static bool HasPathToBoss(
        MapNode currentNode,
        MapNode bossNode,
        IReadOnlyCollection<MapNode> nodes)
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

        foreach (var node in room.Nodes.Where(node => !node.IsBoss))
        {
            room.Nodes
                .Any(candidate => candidate.ParentNodeIds.Contains(node.Id))
                .Should()
                .BeTrue();
        }
    }

    [Fact]
    public void GenerateInitialRoom_ShouldPlaceSingleRoomBossNodeAtFinalRow()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        var finalRowNodes = room.Nodes
            .Where(node => node.Row == room.MaxNodeDepth)
            .ToArray();

        finalRowNodes.Should().ContainSingle();

        var bossNode = finalRowNodes.Single();

        bossNode.IsBoss.Should().BeTrue();
        bossNode.EventType.Should().Be(NodeEventType.RoomBoss);
        bossNode.State.Should().Be(NodeState.Planned);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldCreateAtLeastTwoAvailableNodesAtInitialRow()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.AvailableNodes.Should().HaveCount(2);
        room.AvailableNodes.Should().OnlyContain(node => node.Row == 0);
        room.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public void GenerateNextRoom_ShouldUseMarkovMatrixVersion()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        generator.MarkovMatrixVersion.Should().Be("markov-room-type-0.1.0");
    }
}