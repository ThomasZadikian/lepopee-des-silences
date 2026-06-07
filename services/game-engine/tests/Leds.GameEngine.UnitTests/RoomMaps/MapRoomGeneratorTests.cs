using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class MapRoomGeneratorTests
{
    private const string Seed = "seed-room-map-generator-tests";
    private const string GeneratorVersion = "room-map-layout-1.0.0";

    private static IMapRoomGenerator CreateSut()
    {
        return new MapRoomGenerator(
            new RoomMapLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver());
    }

    [Fact]
    public void GenerateRoom_ShouldUseDefaultRoomMapLayoutTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
        room.TotalNodeCount.Should().Be(22);
    }

    [Fact]
    public void GenerateRoom_ShouldCreateExactlyTwentyTwoMapNodes_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().HaveCount(22);
    }

    [Fact]
    public void GenerateRoom_ShouldCreateFixedDefaultRowDistribution()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var rows = room.Nodes
            .GroupBy(n => n.Row)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToArray();

        rows.Should().Equal(2, 3, 4, 3, 4, 3, 2, 1);
    }

    [Fact]
    public void GenerateRoom_ShouldCreateExactlyTwoInitialNodes_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var initialNodes = room.Nodes.Where(n => n.Row == 0).ToArray();
        initialNodes.Should().HaveCount(2);
        initialNodes.Should().AllSatisfy(n => n.State.Should().Be(NodeState.Available));
    }

    [Fact]
    public void GenerateRoom_ShouldPlaceBossAloneOnFinalRow_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var bossRow = room.Nodes.Max(n => n.Row);
        bossRow.Should().Be(7);

        var finalRowNodes = room.Nodes.Where(n => n.Row == bossRow).ToArray();
        finalRowNodes.Should().HaveCount(1);
        finalRowNodes.Single().IsBoss.Should().BeTrue();
        finalRowNodes.Single().State.Should().Be(NodeState.Planned);
    }

    [Fact]
    public void GenerateRoom_ShouldPersistLayoutTemplateKeyAndVersion()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
    }

    [Fact]
    public void GenerateRoom_ShouldCreateAcyclicMapGraph()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var nodesById = room.Nodes.ToDictionary(n => n.Id);

        foreach (var node in room.Nodes)
        {
            foreach (var parentId in node.ParentNodeIds)
            {
                if (nodesById.TryGetValue(parentId, out var parent))
                {
                    parent.Row.Should().BeLessThan(node.Row,
                        $"Graph must be acyclic: node at row {node.Row} has a parent at row {parent.Row}.");
                }
            }
        }
    }

    [Fact]
    public void GenerateRoom_ShouldMakeEveryNodeReachBoss()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var bossNode = room.Nodes.Single(n => n.IsBoss);

        foreach (var node in room.Nodes.Where(n => !n.IsBoss))
        {
            HasPathToBoss(node, bossNode, room.Nodes).Should().BeTrue(
                $"Node at row {node.Row} lane {node.Lane} should have a path to the boss.");
        }
    }

    [Fact]
    public void GenerateRoom_ShouldNotCreateDeadBranches()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var maxRow = room.Nodes.Max(n => n.Row);

        foreach (var node in room.Nodes.Where(n => !n.IsBoss))
        {
            var hasChild = room.Nodes.Any(candidate =>
                candidate.Row == node.Row + 1 &&
                candidate.ParentNodeIds.Contains(node.Id));

            hasChild.Should().BeTrue(
                $"Non-boss node at row {node.Row} lane {node.Lane} must have at least one child.");
        }
    }

    [Fact]
    public void GenerateRoom_ShouldOnlyConnectToNextRow()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var nodesById = room.Nodes.ToDictionary(n => n.Id);

        foreach (var node in room.Nodes)
        {
            foreach (var parentId in node.ParentNodeIds)
            {
                if (nodesById.TryGetValue(parentId, out var parent))
                {
                    parent.Row.Should().Be(node.Row - 1,
                        $"Node at row {node.Row} should only have parents from row {node.Row - 1}.");
                }
            }
        }
    }

    [Fact]
    public void GenerateRoom_ShouldKeepConnectionsLocalByLane()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var nodesById = room.Nodes.ToDictionary(n => n.Id);

        foreach (var node in room.Nodes)
        {
            foreach (var parentId in node.ParentNodeIds)
            {
                if (nodesById.TryGetValue(parentId, out var parent))
                {
                    var laneDiff = Math.Abs(parent.Lane - node.Lane);
                    laneDiff.Should().BeLessThanOrEqualTo(1,
                        $"Node at lane {node.Lane} should only connect to parent at lane {parent.Lane} (adjacent or same).");
                }
            }
        }
    }

    [Fact]
    public void GenerateRoom_ShouldBeDeterministicForSameSeed()
    {
        var sut = CreateSut();

        var room1 = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));
        var room2 = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));

        var nodeTypes1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var nodeTypes2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        nodeTypes2.Should().Equal(nodeTypes1);
    }

    [Fact]
    public void GenerateRoom_ShouldVaryContentForDifferentSeeds()
    {
        var sut = CreateSut();

        var room1 = sut.Generate("seed-a", GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(1));
        var room2 = sut.Generate("seed-b", GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(2));

        var nodeTypes1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var nodeTypes2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        nodeTypes2.Should().NotEqual(nodeTypes1);
    }

    [Fact]
    public void GenerateRoom_ShouldRespectProvidedLayoutTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
        room.RoomType.Should().Be(RoomType.Threshold);
    }

    [Fact]
    public void GenerateRoom_ShouldNotHaveAllToAllConnections()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var nodesByRow = room.Nodes
            .GroupBy(n => n.Row)
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var row = 0; row < 7; row++)
        {
            if (!nodesByRow.TryGetValue(row, out var currentRow)) continue;
            if (!nodesByRow.TryGetValue(row + 1, out var nextRow)) continue;

            foreach (var parent in currentRow)
            {
                var childLanes = nextRow
                    .Where(c => c.ParentNodeIds.Contains(parent.Id))
                    .Select(c => c.Lane)
                    .ToArray();

                var maxLaneDiff = nextRow.Count - 1;

                if (maxLaneDiff > 1)
                {
                    var hasNonAdjacentConnection = childLanes.Any(lane => Math.Abs(lane - parent.Lane) > 1);
                    hasNonAdjacentConnection.Should().BeFalse(
                        $"Node at row {parent.Row} lane {parent.Lane} should not have all-to-all connections.");
                }
            }
        }
    }

    [Fact]
    public void GenerateRoom_ShouldHaveEveryInitialNodeAsParent()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var initialNodes = room.Nodes.Where(n => n.Row == 0).ToArray();

        foreach (var initialNode in initialNodes)
        {
            var isParent = room.Nodes.Any(n =>
                n.Row == 1 && n.ParentNodeIds.Contains(initialNode.Id));

            isParent.Should().BeTrue(
                $"Initial node at lane {initialNode.Lane} must be a parent of at least one row 1 node.");
        }
    }

    [Fact]
    public void GenerateRoom_ShouldHaveAllNonInitialNodesWithAtLeastOneParent()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        foreach (var node in room.Nodes.Where(n => n.Row > 0))
        {
            node.ParentNodeIds.Should().NotBeEmpty(
                $"Node at row {node.Row} lane {node.Lane} must have at least one parent.");
        }
    }

    private static bool HasPathToBoss(MapNode node, MapNode bossNode, IReadOnlyCollection<MapNode> nodes)
    {
        var children = nodes
            .Where(n => n.ParentNodeIds.Contains(node.Id))
            .ToArray();

        if (children.Any(c => c.Id == bossNode.Id))
        {
            return true;
        }

        return children.Any(c => HasPathToBoss(c, bossNode, nodes));
    }


}
