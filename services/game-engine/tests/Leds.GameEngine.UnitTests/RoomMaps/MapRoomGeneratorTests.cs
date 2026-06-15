using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Catalog;
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
            new RoomBossProfileResolver(new InMemoryCatalogContentGateway()),
            new HardcodedRoomTypeGenerationProfileProvider());
    }

    [Fact]
    public async Task GenerateRoom_ShouldUseDefaultRoomMapLayoutTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
        room.TotalNodeCount.Should().Be(22);
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateExactlyTwentyTwoMapNodes_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().HaveCount(22);
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateFixedDefaultRowDistribution()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var rows = room.Nodes
            .GroupBy(n => n.Row)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToArray();

        rows.Should().Equal(2, 3, 4, 3, 4, 3, 2, 1);
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateExactlyTwoInitialNodes_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var initialNodes = room.Nodes.Where(n => n.Row == 0).ToArray();
        initialNodes.Should().HaveCount(2);
        initialNodes.Should().AllSatisfy(n => n.State.Should().Be(NodeState.Available));
    }

    [Fact]
    public async Task GenerateRoom_ShouldPlaceBossAloneOnFinalRow_WithDefaultTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var bossRow = room.Nodes.Max(n => n.Row);
        bossRow.Should().Be(7);

        var finalRowNodes = room.Nodes.Where(n => n.Row == bossRow).ToArray();
        finalRowNodes.Should().HaveCount(1);
        finalRowNodes.Single().IsBoss.Should().BeTrue();
        finalRowNodes.Single().State.Should().Be(NodeState.Planned);
    }

    [Fact]
    public async Task GenerateRoom_ShouldPersistLayoutTemplateKeyAndVersion()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateAcyclicMapGraph()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldMakeEveryNodeReachBoss()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var bossNode = room.Nodes.Single(n => n.IsBoss);

        foreach (var node in room.Nodes.Where(n => !n.IsBoss))
        {
            HasPathToBoss(node, bossNode, room.Nodes).Should().BeTrue(
                $"Node at row {node.Row} lane {node.Lane} should have a path to the boss.");
        }
    }

    [Fact]
    public async Task GenerateRoom_ShouldNotCreateDeadBranches()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldOnlyConnectToNextRow()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldKeepConnectionsLocalByLane()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldBeDeterministicForSameSeed()
    {
        var sut = CreateSut();

        var room1 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));
        var room2 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));

        var nodeTypes1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var nodeTypes2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        nodeTypes2.Should().Equal(nodeTypes1);
    }

    [Fact]
    public async Task GenerateRoom_ShouldVaryContentForDifferentSeeds()
    {
        var sut = CreateSut();

        var room1 = await sut.GenerateAsync("seed-a", GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(1));
        var room2 = await sut.GenerateAsync("seed-b", GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(2));

        var nodeTypes1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var nodeTypes2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        nodeTypes2.Should().NotEqual(nodeTypes1);
    }

    [Fact]
    public async Task GenerateRoom_ShouldRespectProvidedLayoutTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("threshold-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
        room.RoomType.Should().Be(RoomType.Threshold);
    }

    [Fact]
    public async Task GenerateRoom_ShouldNotHaveAllToAllConnections()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldHaveEveryInitialNodeAsParent()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldHaveAllNonInitialNodesWithAtLeastOneParent()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

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
    public async Task GenerateRoom_ShouldCreate22Nodes_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        room.Nodes.Should().HaveCount(22,
            because: $"all room types use the default 22-node template.");
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public async Task GenerateRoom_ShouldHaveCorrectRowDistribution_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

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
    public async Task GenerateRoom_ShouldHaveSingleBossOnFinalRow_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

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
    public async Task GenerateRoom_ShouldMatchRoomType_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();
        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(42));

        room.RoomType.Should().Be(roomType);
    }

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public async Task GenerateRoom_ShouldBeDeterministic_ForAllSupportedRoomTypes(RoomType roomType)
    {
        var sut = CreateSut();

        var room1 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(99));
        var room2 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, roomType, new Random(99));

        var types1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();
        var types2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane).Select(n => n.EventType).ToArray();

        types2.Should().Equal(types1,
            because: $"same seed + roomType must always produce the same result.");
    }

    // -----------------------------------------------------------------------
    // Differentiation tests — statistical over many nodes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateRoom_ShouldProduceDifferentNodeTypeDistribution_ForDifferentRoomTypes()
    {
        var sut = CreateSut();

        // Discriminator: Elite+Rare+Curse weight = 15% in Threshold vs 50% in Rupture.
        var thresholdTypes = await GenerateManyNodes(sut, RoomType.Threshold);
        var ruptureTypes = await GenerateManyNodes(sut, RoomType.Rupture);

        var IsRisky = (NodeEventType t) =>
            t == NodeEventType.Elite || t == NodeEventType.Rare || t == NodeEventType.Curse;

        var thresholdRiskyRatio = thresholdTypes.Count(IsRisky) / (double)thresholdTypes.Count;
        var ruptureRiskyRatio = ruptureTypes.Count(IsRisky) / (double)ruptureTypes.Count;

        ruptureRiskyRatio.Should().BeGreaterThan(thresholdRiskyRatio + 0.20,
            because: "Rupture has Elite+Rare+Curse at ~50%; Threshold only at ~15%.");
    }

    [Fact]
    public async Task GenerateRuptureRoom_ShouldFavorRiskierNodes()
    {
        var sut = CreateSut();

        var types = await GenerateManyNodes(sut, RoomType.Rupture);

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
    public async Task GenerateForestRoom_ShouldFavorSupportOrNarrativeNodes()
    {
        var sut = CreateSut();

        var types = await GenerateManyNodes(sut, RoomType.Forest);

        var supportCount = types.Count(t =>
            t == NodeEventType.Npc ||
            t == NodeEventType.Law ||
            t == NodeEventType.Rest ||
            t == NodeEventType.Item);

        var supportRatio = supportCount / (double)types.Count;

        supportRatio.Should().BeGreaterThan(0.55,
            because: "Forest favours Npc, Rest, Item — combined weight > 55%.");
    }

    [Fact]
    public async Task GenerateSilenceRoom_ShouldFavorLawNpcOrMerchantNodes()
    {
        var sut = CreateSut();

        var types = await GenerateManyNodes(sut, RoomType.Silence);

        var silenceCount = types.Count(t =>
            t == NodeEventType.Law ||
            t == NodeEventType.Npc ||
            t == NodeEventType.Merchant);

        var silenceRatio = silenceCount / (double)types.Count;

        silenceRatio.Should().BeGreaterThan(0.55,
            because: "Silence favours Law, Npc, Merchant — combined weight > 55%.");
    }

    [Fact]
    public async Task GenerateMemoryRoom_ShouldFavorNpcLawItemOrRestNodes()
    {
        var sut = CreateSut();

        var types = await GenerateManyNodes(sut, RoomType.Memory);

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
    public async Task GenerateMemoryRoom_ShouldNotGenerateDirectMemoryOrNarrativeMapNodes_WhenNotSupported()
    {
        var sut = CreateSut();

        var types = await GenerateManyNodes(sut, RoomType.Memory);

        types.Should().NotContain(NodeEventType.Memory,
            because: "NodeEventType.Memory is not a supported MapNode type in the current system.");
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generates non-boss nodes across many seeds to get a statistically reliable sample.
    /// </summary>
    private static async Task<List<NodeEventType>> GenerateManyNodes(IMapRoomGenerator sut, RoomType roomType, int roomCount = 30)
    {
        var types = new List<NodeEventType>();

        for (var i = 0; i < roomCount; i++)
        {
            var room = await sut.GenerateAsync($"seed-stat-{i}", GeneratorVersion, roomDepth: 0, roomType, new Random(i));
            types.AddRange(room.Nodes.Where(n => !n.IsBoss).Select(n => n.EventType));
        }

        return types;
    }

    // -----------------------------------------------------------------------
    // Boss profile per RoomType
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateThresholdRoom_ShouldUseThresholdBossProfile()
    {
        var room = await CreateSut().GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));

        room.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.BossId.Should().Be("boss.threshold.warden");
        room.BossProfile.Name.Should().Be("Gardien du Seuil");
        room.BossProfile.RoomType.Should().Be(RoomType.Threshold);
        room.BossProfile.DangerHint.Should().Be("High");
        room.BossProfile.EnemyTemplateKey.Should().Be("boss.threshold.warden-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public async Task GenerateForestRoom_ShouldUseForestBossProfile()
    {
        var room = await CreateSut().GenerateAsync(Seed, GeneratorVersion, roomDepth: 1, RoomType.Forest, new Random(42));

        room.RoomType.Should().Be(RoomType.Forest);
        room.BossProfile.BossId.Should().Be("boss.forest.rootbound-memory");
        room.BossProfile.Name.Should().Be("Gardien des Racines");
        room.BossProfile.RoomType.Should().Be(RoomType.Forest);
        room.BossProfile.DangerHint.Should().Be("Medium");
        room.BossProfile.EnemyTemplateKey.Should().Be("boss.forest.rootbound-memory-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public async Task GenerateRuptureRoom_ShouldUseRuptureBossProfile()
    {
        var room = await CreateSut().GenerateAsync(Seed, GeneratorVersion, roomDepth: 1, RoomType.Rupture, new Random(42));

        room.RoomType.Should().Be(RoomType.Rupture);
        room.BossProfile.BossId.Should().Be("boss.rupture.fractured-echo");
        room.BossProfile.Name.Should().Be("Fragment de Rupture");
        room.BossProfile.RoomType.Should().Be(RoomType.Rupture);
        room.BossProfile.DangerHint.Should().Be("High");
        room.BossProfile.EnemyTemplateKey.Should().Be("boss.rupture.fractured-echo-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public async Task GenerateSilenceRoom_ShouldUseSilenceBossProfile()
    {
        var room = await CreateSut().GenerateAsync(Seed, GeneratorVersion, roomDepth: 1, RoomType.Silence, new Random(42));

        room.RoomType.Should().Be(RoomType.Silence);
        room.BossProfile.BossId.Should().Be("boss.silence.mute-herald");
        room.BossProfile.Name.Should().Be("Voix Éteinte");
        room.BossProfile.RoomType.Should().Be(RoomType.Silence);
        room.BossProfile.DangerHint.Should().Be("Medium");
        room.BossProfile.EnemyTemplateKey.Should().Be("boss.silence.mute-herald-v1");
        AssertSingleBossOnFinalRow(room);
    }

    [Fact]
    public async Task GenerateMemoryRoom_ShouldUseMemoryBossProfile()
    {
        var room = await CreateSut().GenerateAsync(Seed, GeneratorVersion, roomDepth: 1, RoomType.Memory, new Random(42));

        room.RoomType.Should().Be(RoomType.Memory);
        room.BossProfile.BossId.Should().Be("boss.memory.archivist");
        room.BossProfile.Name.Should().Be("Archiviste des Échos");
        room.BossProfile.RoomType.Should().Be(RoomType.Memory);
        room.BossProfile.DangerHint.Should().Be("Medium");
        room.BossProfile.EnemyTemplateKey.Should().Be("boss.memory.archivist-v1");
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

    // -----------------------------------------------------------------------
    // B-group — per-RoomType distribution tests (PR 0.1.8)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateThresholdRoom_ShouldUseBalancedNodeDistribution()
    {
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Threshold);

        // Combat is the dominant type (weight 30 / 100).
        var combatCount = types.Count(t => t == NodeEventType.Combat);
        var combatRatio = combatCount / (double)types.Count;

        combatRatio.Should().BeGreaterThan(0.20,
            "Threshold Combat weight is 30 %; sample ratio should exceed 20 %.");

        // No single risky type should dominate: Elite, Curse each ≤ 10% sample
        var eliteRatio = types.Count(t => t == NodeEventType.Elite) / (double)types.Count;
        var curseRatio = types.Count(t => t == NodeEventType.Curse) / (double)types.Count;

        eliteRatio.Should().BeLessThan(0.15,
            "Elite weight is 5 % in Threshold — sample ratio should stay well below 15 %.");
        curseRatio.Should().BeLessThan(0.15);
    }

    [Fact]
    public async Task GenerateForestRoom_ShouldGenerateSupportOrExplorationNodes()
    {
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Forest);

        var supportCount = types.Count(t =>
            t == NodeEventType.Npc ||
            t == NodeEventType.Law ||
            t == NodeEventType.Rest ||
            t == NodeEventType.Item);

        (supportCount / (double)types.Count).Should().BeGreaterThan(0.55,
            "Forest favours Npc, Rest, Item — combined weight is 65 %.");
    }

    [Fact]
    public async Task GenerateRuptureRoom_ShouldGenerateRiskierNodes()
    {
        // Alias for existing test; kept as a named B-group entry.
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Rupture);

        var riskyCount = types.Count(t =>
            t == NodeEventType.Combat ||
            t == NodeEventType.Elite ||
            t == NodeEventType.Rare ||
            t == NodeEventType.Curse);

        (riskyCount / (double)types.Count).Should().BeGreaterThan(0.60);
    }

    [Fact]
    public async Task GenerateSilenceRoom_ShouldGenerateSystemicNodes()
    {
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Silence);

        var systemicCount = types.Count(t =>
            t == NodeEventType.Law ||
            t == NodeEventType.Npc ||
            t == NodeEventType.Merchant);

        (systemicCount / (double)types.Count).Should().BeGreaterThan(0.55,
            "Silence favours Law, Npc, Merchant — combined weight is 70 %.");
    }

    [Fact]
    public async Task GenerateMemoryRoom_ShouldGenerateMemoryLikeButNotDirectMemoryMapNodes()
    {
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Memory);

        // Must NOT contain direct Memory node type
        types.Should().NotContain(NodeEventType.Memory,
            "NodeEventType.Memory is not a supported direct MapNode type.");

        // Npc+Law+Item+Rest must dominate
        var likeMemory = types.Count(t =>
            t == NodeEventType.Npc ||
            t == NodeEventType.Law ||
            t == NodeEventType.Item ||
            t == NodeEventType.Rest);

        (likeMemory / (double)types.Count).Should().BeGreaterThan(0.80);
    }

    // -----------------------------------------------------------------------
    // C-group — Memory/Narrative non-regression (PR 0.1.8)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateMemoryRoom_ShouldNotGenerateDirectMemoryMapNodes()
    {
        var sut = CreateSut();
        var types = await GenerateManyNodes(sut, RoomType.Memory);

        types.Should().NotContain(NodeEventType.Memory,
            "NodeEventType.Memory must never appear as a direct MapNode type.");
    }

    // NodeEventType.Narrative does not exist in the current enum — test not applicable.
    // If added in the future, add: types.Should().NotContain(NodeEventType.Narrative)

    // -----------------------------------------------------------------------
    // D-group — risk level comparison with known seeds (PR 0.1.8)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateRuptureRoom_ShouldHaveHigherAverageRiskThanThreshold_ForKnownSeed()
    {
        var sut = CreateSut();

        var thresholdRisk = await AverageNonBossRisk(sut, RoomType.Threshold);
        var ruptureRisk = await AverageNonBossRisk(sut, RoomType.Rupture);

        ruptureRisk.Should().BeGreaterThan(thresholdRisk,
            "Rupture riskMin=25 versus Threshold riskMin=5 must yield a higher average risk.");
    }

    [Fact]
    public async Task GenerateForestRoom_ShouldHaveLowerOrModerateAverageRisk_ForKnownSeed()
    {
        var sut = CreateSut();

        var forestRisk = await AverageNonBossRisk(sut, RoomType.Forest);
        var ruptureRisk = await AverageNonBossRisk(sut, RoomType.Rupture);

        forestRisk.Should().BeLessThan(ruptureRisk,
            "Forest riskMax=51 versus Rupture riskMax=86 must yield a lower average risk.");
    }

    [Fact]
    public async Task GenerateThresholdRoom_ShouldKeepIntroRiskModerate_ForKnownSeed()
    {
        var sut = CreateSut();
        var room = await sut.GenerateAsync(Seed, GeneratorVersion, 0, RoomType.Threshold, new Random(42));

        var nonBossRisks = room.Nodes.Where(n => !n.IsBoss).Select(n => n.RiskLevel).ToArray();

        nonBossRisks.Should().AllSatisfy(r =>
            r.Should().BeInRange(5, 60,
                "Threshold risk is bounded to [5, 61) — no node should exceed 60."));
    }

    private static async Task<double> AverageNonBossRisk(IMapRoomGenerator sut, RoomType roomType, int roomCount = 10)
    {
        var risks = new List<int>();
        for (var i = 0; i < roomCount; i++)
        {
            var room = await sut.GenerateAsync($"seed-risk-{i}", GeneratorVersion, 0, roomType, new Random(i * 7 + 3));
            risks.AddRange(room.Nodes.Where(n => !n.IsBoss).Select(n => n.RiskLevel));
        }
        return risks.Average();
    }

    // -----------------------------------------------------------------------
    // E-group — reward profile assertions per node type (PR 0.1.8)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateCombatNode_ShouldUseRoomTypeAwareRewardProfile()
    {
        var sut = CreateSut();

        // Threshold: only "combat-common"
        var thresholdCombatNodes = await GenerateManyMapNodes(sut, RoomType.Threshold);
        var thresholdCombat = thresholdCombatNodes
            .Where(n => n.EventType == NodeEventType.Combat).ToList();

        thresholdCombat.Should().NotBeEmpty("Threshold has 30% Combat weight — some combat nodes expected.");
        thresholdCombat.Should().AllSatisfy(n =>
            n.RewardProfile.Should().Be("combat-common",
                "Threshold combat nodes use only 'combat-common'."));

        // Rupture: "combat-common" or "combat-uncommon"
        var ruptureCombatNodes = await GenerateManyMapNodes(sut, RoomType.Rupture);
        var ruptureCombat = ruptureCombatNodes
            .Where(n => n.EventType == NodeEventType.Combat).ToList();

        ruptureCombat.Should().NotBeEmpty();
        ruptureCombat.Should().AllSatisfy(n =>
            n.RewardProfile.Should().BeOneOf(new[] { "combat-common", "combat-uncommon" },
                "Rupture combat nodes use risk-aware reward profiles."));
    }

    [Fact]
    public async Task GenerateRareNode_ShouldUseRareCombatRewardProfile()
    {
        var sut = CreateSut();

        // Sample across all room types (Rare exists in all profiles)
        var rareNodeLists = await Task.WhenAll(
            new[] { RoomType.Threshold, RoomType.Forest, RoomType.Rupture, RoomType.Silence, RoomType.Memory }
                .Select(rt => GenerateManyMapNodes(sut, rt)));
        var rareNodes = rareNodeLists.SelectMany(x => x)
            .Where(n => n.EventType == NodeEventType.Rare)
            .ToList();

        rareNodes.Should().NotBeEmpty("At least some rare nodes must appear across all room types.");
        rareNodes.Should().AllSatisfy(n =>
            n.RewardProfile.Should().StartWith("rare",
                "Rare nodes must always use a reward profile starting with 'rare'."));
    }

    [Fact]
    public async Task GenerateEliteNode_ShouldUseEliteCombatRewardProfile()
    {
        var sut = CreateSut();

        var eliteNodeLists = await Task.WhenAll(
            new[] { RoomType.Threshold, RoomType.Rupture }
                .Select(rt => GenerateManyMapNodes(sut, rt)));
        var eliteNodes = eliteNodeLists.SelectMany(x => x)
            .Where(n => n.EventType == NodeEventType.Elite)
            .ToList();

        eliteNodes.Should().NotBeEmpty("Threshold and Rupture both have Elite weight — some elite nodes expected.");
        eliteNodes.Should().AllSatisfy(n =>
            n.RewardProfile.Should().StartWith("elite",
                "Elite nodes must always use a reward profile starting with 'elite'."));
    }

    [Fact]
    public async Task GenerateRestNode_ShouldUseRestRewardProfile()
    {
        var sut = CreateSut();

        var restNodeLists = await Task.WhenAll(
            new[] { RoomType.Threshold, RoomType.Forest, RoomType.Rupture }
                .Select(rt => GenerateManyMapNodes(sut, rt)));
        var restNodes = restNodeLists.SelectMany(x => x)
            .Where(n => n.EventType == NodeEventType.Rest)
            .ToList();

        restNodes.Should().NotBeEmpty();
        restNodes.Should().AllSatisfy(n =>
            n.RewardProfile.Should().StartWith("rest",
                "Rest nodes must use a 'rest-*' reward profile."));
    }

    [Fact]
    public async Task GenerateCurseNode_ShouldUseRiskRewardProfile()
    {
        var sut = CreateSut();

        // Rupture and Threshold both include Curse
        var curseNodeLists = await Task.WhenAll(
            new[] { RoomType.Rupture, RoomType.Threshold }
                .Select(rt => GenerateManyMapNodes(sut, rt)));
        var curseNodes = curseNodeLists.SelectMany(x => x)
            .Where(n => n.EventType == NodeEventType.Curse)
            .ToList();

        curseNodes.Should().NotBeEmpty("Rupture has 15% Curse weight — some curse nodes expected.");
        curseNodes.Should().AllSatisfy(n =>
            n.RewardProfile.Should().StartWith("curse",
                "Curse nodes must use a 'curse-*' reward profile."));
    }

    // -----------------------------------------------------------------------
    // Helper — returns MapNode objects (with RewardProfile) across many seeds
    // -----------------------------------------------------------------------

    private static async Task<List<MapNode>> GenerateManyMapNodes(IMapRoomGenerator sut, RoomType roomType, int roomCount = 30)
    {
        var nodes = new List<MapNode>();
        for (var i = 0; i < roomCount; i++)
        {
            var room = await sut.GenerateAsync($"seed-stat-{i}", GeneratorVersion, 0, roomType, new Random(i));
            nodes.AddRange(room.Nodes.Where(n => !n.IsBoss));
        }
        return nodes;
    }
}