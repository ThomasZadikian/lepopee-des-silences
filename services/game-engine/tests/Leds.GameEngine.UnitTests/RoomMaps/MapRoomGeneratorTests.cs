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
            new RoomBossProfileResolver(),
            new HardcodedRoomTypeGenerationProfileProvider());
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

    // -----------------------------------------------------------------------
    // Per-RoomType structure tests
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GenerateRoom_ShouldCreate22Nodes_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        room.Nodes.Should().HaveCount(22,
            because: $"all room types use the default 22-node template.");
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GenerateRoom_ShouldHaveCorrectRowDistribution_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        var rows = room.Nodes
            .GroupBy(n => n.Row)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToArray();

        rows.Should().Equal(new[] { 2, 3, 4, 3, 4, 3, 2, 1 },
            "the layout template is the same for all room types.");
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GenerateRoom_ShouldHaveSingleBossOnFinalRow_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        var bossNodes = room.Nodes.Where(n => n.IsBoss).ToArray();
        bossNodes.Should().HaveCount(1);
        bossNodes.Single().EventType.Should().Be(NodeEventType.RoomBoss);
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GenerateRoom_ShouldMatchRoomType_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        room.RoomType.Should().Be(roomType);
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GenerateRoom_ShouldBeDeterministic_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();

        var room1 = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(99));
        var room2 = sut.Generate(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(99));

        var types1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var types2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        types2.Should().Equal(types1,
            because: $"same seed + roomType must always produce the same result.");
    }

    // -----------------------------------------------------------------------
    // Differentiation tests — statistical over many nodes
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateRoom_ShouldProduceDifferentNodeTypeDistribution_ForDifferentRoomTypes()
    {
        var sut = CreateSut();

        // Discriminator: Elite+Rare+Curse weight = 15% in Threshold vs 50% in Rupture.
        var thresholdTypes = GenerateManyNodes(sut, RoomType.Threshold);
        var ruptureTypes = GenerateManyNodes(sut, RoomType.Rupture);

        var IsRisky = (NodeEventType t) =>
            t == NodeEventType.Elite || t == NodeEventType.Rare || t == NodeEventType.Curse;

        var thresholdRiskyRatio = thresholdTypes.Count(IsRisky) / (double)thresholdTypes.Count;
        var ruptureRiskyRatio = ruptureTypes.Count(IsRisky) / (double)ruptureTypes.Count;

        ruptureRiskyRatio.Should().BeGreaterThan(thresholdRiskyRatio + 0.20,
            because: "Rupture has Elite+Rare+Curse at ~50%; Threshold only at ~15%.");
    }

    [Fact]
    public void GenerateRuptureRoom_ShouldFavorRiskierNodes()
    {
        var sut = CreateSut();

        var types = GenerateManyNodes(sut, RoomType.Rupture);

        var riskyCount = types.Count(t =>
            t == NodeEventType.Combat ||
            t == NodeEventType.Elite ||
            t == NodeEventType.Rare ||
            t == NodeEventType.Curse);

        var riskyRatio = riskyCount / (double)types.Count;

        riskyRatio.Should().BeGreaterThan(0.60,
            because: "Rupture favours Combat, Elite, Rare, Curse — combined weight > 60%.");
    }

    [Fact]
    public void GenerateForestRoom_ShouldFavorSupportOrNarrativeNodes()
    {
        var sut = CreateSut();

        var types = GenerateManyNodes(sut, RoomType.Forest);

        var supportCount = types.Count(t =>
            t == NodeEventType.Npc ||
            t == NodeEventType.Rest ||
            t == NodeEventType.Item);

        var supportRatio = supportCount / (double)types.Count;

        supportRatio.Should().BeGreaterThan(0.55,
            because: "Forest favours Npc, Rest, Item — combined weight > 55%.");
    }

    [Fact]
    public void GenerateSilenceRoom_ShouldFavorLawNpcOrMerchantNodes()
    {
        var sut = CreateSut();

        var types = GenerateManyNodes(sut, RoomType.Silence);

        var silenceCount = types.Count(t =>
            t == NodeEventType.Law ||
            t == NodeEventType.Npc ||
            t == NodeEventType.Merchant);

        var silenceRatio = silenceCount / (double)types.Count;

        silenceRatio.Should().BeGreaterThan(0.55,
            because: "Silence favours Law, Npc, Merchant — combined weight > 55%.");
    }

    [Fact]
    public void GenerateMemoryRoom_ShouldFavorNpcLawItemOrRestNodes()
    {
        var sut = CreateSut();

        var types = GenerateManyNodes(sut, RoomType.Memory);

        var memoryCount = types.Count(t =>
            t == NodeEventType.Npc ||
            t == NodeEventType.Law ||
            t == NodeEventType.Item ||
            t == NodeEventType.Rest);

        var memoryRatio = memoryCount / (double)types.Count;

        memoryRatio.Should().BeGreaterThan(0.80,
            because: "Memory favours Npc, Law, Item, Rest — combined weight > 80%.");
    }

    [Fact]
    public void GenerateMemoryRoom_ShouldNotGenerateDirectMemoryOrNarrativeMapNodes_WhenNotSupported()
    {
        var sut = CreateSut();

        var types = GenerateManyNodes(sut, RoomType.Memory);

        types.Should().NotContain(NodeEventType.Memory,
            because: "NodeEventType.Memory is not a supported MapNode type in the current system.");
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generates non-boss nodes across many seeds to get a statistically reliable sample.
    /// </summary>
    private static List<NodeEventType> GenerateManyNodes(IMapRoomGenerator sut, RoomType roomType, int roomCount = 30)
    {
        var types = new List<NodeEventType>();

        for (var i = 0; i < roomCount; i++)
        {
            var room = sut.Generate($"seed-stat-{i}", GeneratorVersion, roomDepth: 0, roomType, new Random(i));
            types.AddRange(room.Nodes.Where(n => !n.IsBoss).Select(n => n.EventType));
        }

        return types;
    }

    // -----------------------------------------------------------------------
    // Boss profile per RoomType
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateThresholdRoom_ShouldUseThresholdBossProfile()
    {
        var room = CreateSut().Generate(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));

        room.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.BossId.Should().Be("threshold-guardian");
        room.BossProfile.Name.Should().Be("Gardien du Seuil");
        room.BossProfile.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.DangerHint.Should().NotBeNullOrWhiteSpace();
        room.BossProfile.EnemyTemplateKey.Should().Be("boss-threshold-guardian-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public void GenerateForestRoom_ShouldUseForestBossProfile()
    {
        var room = CreateSut().Generate(Seed, GeneratorVersion, roomDepth: 1, RoomType.Forest, new Random(42));

        room.RoomType.Should().Be(RoomType.Forest);
        room.BossProfile.BossId.Should().Be("forest-guardian");
        room.BossProfile.Name.Should().Be("Gardien des Racines");
        room.BossProfile.RoomType.Should().Be(RoomType.Forest);
        room.BossProfile.DangerHint.Should().NotBeNullOrWhiteSpace();
        room.BossProfile.EnemyTemplateKey.Should().Be("boss-forest-guardian-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public void GenerateRuptureRoom_ShouldUseRuptureBossProfile()
    {
        var room = CreateSut().Generate(Seed, GeneratorVersion, roomDepth: 1, RoomType.Rupture, new Random(42));

        room.RoomType.Should().Be(RoomType.Rupture);
        room.BossProfile.BossId.Should().Be("rupture-warden");
        room.BossProfile.Name.Should().Be("Fragment de Rupture");
        room.BossProfile.RoomType.Should().Be(RoomType.Rupture);
        room.BossProfile.DangerHint.Should().NotBeNullOrWhiteSpace();
        room.BossProfile.EnemyTemplateKey.Should().Be("boss-rupture-warden-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public void GenerateSilenceRoom_ShouldUseSilenceBossProfile()
    {
        var room = CreateSut().Generate(Seed, GeneratorVersion, roomDepth: 1, RoomType.Silence, new Random(42));

        room.RoomType.Should().Be(RoomType.Silence);
        room.BossProfile.BossId.Should().Be("silence-warden");
        room.BossProfile.Name.Should().Be("Voix Éteinte");
        room.BossProfile.RoomType.Should().Be(RoomType.Silence);
        room.BossProfile.DangerHint.Should().NotBeNullOrWhiteSpace();
        room.BossProfile.EnemyTemplateKey.Should().Be("boss-silence-warden-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public void GenerateMemoryRoom_ShouldUseMemoryBossProfile()
    {
        var room = CreateSut().Generate(Seed, GeneratorVersion, roomDepth: 1, RoomType.Memory, new Random(42));

        room.RoomType.Should().Be(RoomType.Memory);
        room.BossProfile.BossId.Should().Be("memory-keeper");
        room.BossProfile.Name.Should().Be("Archiviste des Échos");
        room.BossProfile.RoomType.Should().Be(RoomType.Memory);
        room.BossProfile.DangerHint.Should().NotBeNullOrWhiteSpace();
        room.BossProfile.EnemyTemplateKey.Should().Be("boss-memory-keeper-v1");
        AssertSingleBossOnFinalRow(room);
    }

    private static void AssertSingleBossOnFinalRow(Room room)
    {
        var bossNodes = room.Nodes.Where(n => n.IsBoss).ToArray();
        bossNodes.Should().HaveCount(1, "there must be exactly one boss node.");
        bossNodes.Single().EventType.Should().Be(NodeEventType.RoomBoss);

        var finalRow = room.Nodes.Max(n => n.Row);
        room.Nodes.Where(n => n.Row == finalRow).Should().HaveCount(1,
            "the boss must be alone on the final row.");
    }
}
