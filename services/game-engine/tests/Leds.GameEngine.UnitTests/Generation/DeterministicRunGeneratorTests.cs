using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public async Task GenerateInitialRoom_ShouldCreateVisibleRoomPlan()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

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
    public async Task GenerateInitialRoom_ShouldCreateRoomBossMatchingRoomType()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        room.BossProfile.Should().NotBeNull();
        room.BossProfile.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.BossId.Should().Be("boss.threshold.warden");
        room.BossProfile.Name.Should().Be("Gardien du Seuil");
        room.BossProfile.DangerHint.Should().Be("High");

        var bossNode = room.Nodes.Single(node => node.IsBoss);

        bossNode.Row.Should().Be(room.MaxNodeDepth);
        bossNode.State.Should().Be(NodeState.Planned);
        bossNode.EventType.Should().Be(NodeEventType.RoomBoss);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldCreateEightRows()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        var rowCount = room.Nodes
            .Select(node => node.Row)
            .Distinct()
            .Count();

        rowCount.Should().Be(8);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldBeDeterministic()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var firstRoom = await generator.GenerateInitialRoomAsync("seed-test-001");
        var secondRoom = await generator.GenerateInitialRoomAsync("seed-test-001");

        var firstSnapshot = CreateRoomPlanSnapshot(firstRoom);
        var secondSnapshot = CreateRoomPlanSnapshot(secondRoom);

        secondSnapshot.Should().BeEquivalentTo(
            firstSnapshot,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldGenerateDifferentPlans_ForDifferentSeeds()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var firstRoom = await generator.GenerateInitialRoomAsync("seed-test-001");
        var secondRoom = await generator.GenerateInitialRoomAsync("seed-test-002");

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
    public async Task GenerateInitialRoom_ShouldCreateConvergentGraph_ToRoomBoss()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

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
    public async Task GenerateInitialRoom_ShouldGiveEveryNonBossNodeAtLeastOneChild()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        foreach (var node in room.Nodes.Where(node => !node.IsBoss))
        {
            room.Nodes
                .Any(candidate => candidate.ParentNodeIds.Contains(node.Id))
                .Should()
                .BeTrue();
        }
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldPlaceSingleRoomBossNodeAtFinalRow()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

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
    public async Task GenerateInitialRoom_ShouldCreateAtLeastTwoAvailableNodesAtInitialRow()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        room.AvailableNodes.Should().HaveCount(2);
        room.AvailableNodes.Should().OnlyContain(node => node.Row == 0);
        room.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldHaveNeutralPalaceState()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-palace-test");

        room.PalaceState.Should().Be(PalaceRoomState.Neutral,
            because: "The initial Threshold room at depth 0 always has Neutral state.");
    }

    [Fact]
    public async Task GenerateNextRoom_ShouldResolvePalaceState()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();
        var initialRoom = await generator.GenerateInitialRoomAsync("seed-palace-next");
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-palace-next",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow);

        var nextRoom = await generator.GenerateNextRoomAsync(run);

        nextRoom.PalaceState.Should().NotBe(PalaceRoomState.Enraged,
            because: "Enraged is defined but not yet a candidate state.");
        nextRoom.PalaceState.Should().NotBe(PalaceRoomState.Violent,
            because: "Violent is defined but not yet a candidate state.");
        nextRoom.PalaceState.Should().BeOneOf(
            PalaceRoomState.Neutral,
            PalaceRoomState.Silent,
            PalaceRoomState.Painful);
    }

    [Fact]
    public async Task GenerateNextRoom_ShouldBeDeterministic_ForPalaceState()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();
        var initialRoom = await generator.GenerateInitialRoomAsync("seed-palace-det");
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-palace-det",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow);

        var nextRoomA = await generator.GenerateNextRoomAsync(run);
        var nextRoomB = await generator.GenerateNextRoomAsync(run);

        nextRoomA.PalaceState.Should().Be(nextRoomB.PalaceState,
            because: "Same run seed and context must produce the same PalaceState deterministically.");
    }

    [Fact]
    public void GenerateNextRoom_ShouldUseMarkovMatrixVersion()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        generator.MarkovMatrixVersion.Should().Be("markov-room-type-0.1.0");
    }
}