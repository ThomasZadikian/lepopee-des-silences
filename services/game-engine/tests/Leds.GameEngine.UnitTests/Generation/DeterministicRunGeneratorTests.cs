using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public async Task GenerateInitialRoom_ShouldAttachStructurelessPalaceNotice_WhenNoWorldIsConfigured()
    {
        // Default stub gateway has no Worlds/RoomDefinitions configured — the legacy
        // fallback path this whole test file otherwise exercises implicitly.
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-no-world");

        room.CatalogBinding.Should().NotBeNull();
        room.CatalogBinding!.DisplayName.Should().BeEmpty(
            because: "an empty DisplayName keeps the top bar showing the theme/room type, not a fake canon name.");
        room.CatalogBinding!.NarrativeText.Should().Be(
            DeterministicRunGenerator.StructurelessPalaceNotice);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldBindTheRealEntryRoom_AndNotAttachTheNotice_WhenAWorldIsConfigured()
    {
        var catalogGateway = new StubCatalogContentGateway
        {
            WorldDefinitions = [new CatalogWorldDefinition("palais", "Palais", "room.halldentree")],
            RoomDefinitions =
            [
                new CatalogRoomDefinition(
                    Key: "room.halldentree",
                    DisplayName: "Hall d'entrée",
                    Description: "Le hall d'entrée du Palais.",
                    NarrativeText: "Un tapis rouge et quatre piliers de marbre.",
                    RoomFamily: "Palais intérieur",
                    RoomRarity: "Epic",
                    Theme: "Welcome",
                    MinDepth: 0,
                    MaxDepth: 9,
                    BaseWeight: 1,
                    EnemyPoolKey: null,
                    RewardPoolKey: null,
                    LawPoolKey: null,
                    CursePoolKey: null,
                    BossDefinitionKey: null,
                    IsUnique: false,
                    WorldKey: "palais",
                    IsWorldEntryRoom: true,
                    TriggersStrictChain: false,
                    ReachableRoomKeys: [])
            ]
        };

        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator(catalogGateway);

        var room = await generator.GenerateInitialRoomAsync("seed-with-world");

        room.CatalogBinding.Should().NotBeNull();
        room.CatalogBinding!.Key.Should().Be("room.halldentree");
        room.CatalogBinding!.DisplayName.Should().Be("Hall d'entrée");
        room.CatalogBinding!.NarrativeText.Should().Be("Un tapis rouge et quatre piliers de marbre.");
        room.CatalogBinding!.NarrativeText.Should().NotBe(
            DeterministicRunGenerator.StructurelessPalaceNotice);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldUseTheEntryRoomsOwnTemplate_WhenItIsCatalogProfiled()
    {
        // Regression guard for the catalog-before-geometry inversion (Chantier 1): the entry
        // room's CatalogRoomDefinition is resolved before geometry generation even runs — this
        // proves that SAME resolved definition (not just its metadata, attached afterward) is
        // what actually drove the grid shape, by asserting on the room-specific template that
        // only a threaded "room.jardin" key could have selected.
        var catalogGateway = new StubCatalogContentGateway
        {
            WorldDefinitions = [new CatalogWorldDefinition("palais", "Palais", "room.jardin")],
            RoomDefinitions =
            [
                new CatalogRoomDefinition(
                    Key: "room.jardin",
                    DisplayName: "Le jardin",
                    Description: "Un jardin clos.",
                    NarrativeText: "Des fourrés et une terrasse plantée.",
                    RoomFamily: "Palais intérieur",
                    RoomRarity: "Common",
                    Theme: "Forest",
                    MinDepth: 0,
                    MaxDepth: 9,
                    BaseWeight: 1,
                    EnemyPoolKey: null,
                    RewardPoolKey: null,
                    LawPoolKey: null,
                    CursePoolKey: null,
                    BossDefinitionKey: null,
                    IsUnique: false,
                    WorldKey: "palais",
                    IsWorldEntryRoom: true,
                    TriggersStrictChain: false,
                    ReachableRoomKeys: [])
            ]
        };

        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator(catalogGateway);

        var room = await generator.GenerateInitialRoomAsync("seed-jardin-entry");

        room.CatalogBinding!.Key.Should().Be("room.jardin");
        room.LayoutTemplateKey.Should().Be("room.jardin-v1");
        room.Grid!.Width.Should().Be(26);
        room.Grid.Height.Should().Be(18);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldGenerateAGridRoom_ForACatalogBoundEntryRoom()
    {
        // Regression: GenerateRoomShapeAsync used to forward the Classic generator version
        // ("room-map-layout-1.0.0") to the grid generator unconditionally, which only knows
        // its own version ("grid-room-layout-1.0.0") — the very first room crashed with
        // KeyNotFoundException regardless of RoomType. Exercised through the catalog-bound
        // entry-room path (the one that actually crashed in production — the entry room's
        // theme doesn't parse as a RoomType, so it resolves to RoomType.Memory via
        // MapThemeToScaffold).
        var catalogGateway = new StubCatalogContentGateway
        {
            WorldDefinitions = [new CatalogWorldDefinition("palais", "Palais", "room.halldentree")],
            RoomDefinitions =
            [
                new CatalogRoomDefinition(
                    Key: "room.halldentree",
                    DisplayName: "Hall d'entrée",
                    Description: "Le hall d'entrée du Palais.",
                    NarrativeText: "Un tapis rouge et quatre piliers de marbre.",
                    RoomFamily: "Palais intérieur",
                    RoomRarity: "Epic",
                    Theme: "Welcome",
                    MinDepth: 0,
                    MaxDepth: 9,
                    BaseWeight: 1,
                    EnemyPoolKey: null,
                    RewardPoolKey: null,
                    LawPoolKey: null,
                    CursePoolKey: null,
                    BossDefinitionKey: null,
                    IsUnique: false,
                    WorldKey: "palais",
                    IsWorldEntryRoom: true,
                    TriggersStrictChain: false,
                    ReachableRoomKeys: [])
            ]
        };

        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator(catalogGateway);

        var room = await generator.GenerateInitialRoomAsync("seed-with-world");

        room.Grid.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldCreateVisibleRoomPlan()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        room.Depth.Should().Be(0);
        room.RoomType.Should().Be(RoomType.Threshold);
        room.Theme.Should().Be("Threshold");
        room.State.Should().Be(RoomState.Active);

        room.TotalNodeCount.Should().BeInRange(22, 30);
        room.Nodes.Should().HaveCount(room.TotalNodeCount);
        room.Nodes.Should().NotContain(node => node.IsBoss);
        room.Nodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public async Task GenerateInitialRoom_ShouldNotInventBossWithoutCatalogDeclaration()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        room.BossProfile.Should().BeNull();
        room.Nodes.Should().NotContain(node => node.IsBoss);
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
    public async Task GenerateInitialRoom_ShouldPlaceEveryNodeOnADistinctCell_WithinGridBounds()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();

        var room = await generator.GenerateInitialRoomAsync("seed-test-001");

        room.Nodes.Select(node => (node.Lane, node.Row)).Distinct().Should().HaveCount(room.Nodes.Count);
        room.Nodes.Should().OnlyContain(node =>
            node.Lane >= 0 && node.Lane < room.Grid.Width &&
            node.Row >= 0 && node.Row < room.Grid.Height);
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
    public async Task GenerateNextRoom_ShouldGenerateAGridRoom()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();
        var initialRoom = await generator.GenerateInitialRoomAsync("seed-next");
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-next",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var nextRoom = await generator.GenerateNextRoomAsync(run);

        nextRoom.Grid.Should().NotBeNull();
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
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

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
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

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

    private static StubCatalogContentGateway CreateWorldGraphGateway()
    {
        // A single-branch chain (each room has exactly one reachable room, so
        // RoomReachabilitySelector's eligible.Length==1 shortcut applies — no RNG tie-break
        // to replicate). "room.c" is a dead end, which loops back to the World's entry room
        // per SFD § 5.4 (ResolveWorldEntryRoom) — exercising the loop is part of the point.
        CatalogRoomDefinition Room(string key, string[] reachable) => new(
            Key: key,
            DisplayName: $"Salle {key}",
            Description: "desc",
            NarrativeText: "narrative",
            RoomFamily: "Palais intérieur",
            RoomRarity: "Common",
            Theme: "Welcome",
            MinDepth: 0,
            MaxDepth: 9,
            BaseWeight: 1,
            EnemyPoolKey: null,
            RewardPoolKey: null,
            LawPoolKey: null,
            CursePoolKey: null,
            BossDefinitionKey: null,
            IsUnique: false,
            WorldKey: "palais",
            IsWorldEntryRoom: key == "room.halldentree",
            TriggersStrictChain: false,
            ReachableRoomKeys: reachable);

        return new StubCatalogContentGateway
        {
            WorldDefinitions = [new CatalogWorldDefinition("palais", "Palais", "room.halldentree")],
            RoomDefinitions =
            [
                Room("room.halldentree", ["room.a"]),
                Room("room.a", ["room.b"]),
                Room("room.b", ["room.c"]),
                Room("room.c", []),
            ]
        };
    }

    /// <summary>
    /// Rebuilds a Run sitting right after <paramref name="rooms"/>' last room, entirely via
    /// <see cref="Run.Rehydrate"/> — bypassing the node/combat/Interlude state machine, which
    /// the preview logic itself never touches (it only reads room identity/history). This
    /// mirrors exactly the information <see cref="DeterministicRunGenerator.GenerateNextRoomAsync"/>
    /// and <see cref="DeterministicRunGenerator.PreviewUpcomingRoomNamesAsync"/> read.
    /// </summary>
    private static Run RehydrateRunAt(string seed, string generatorVersion, string markovMatrixVersion, IReadOnlyList<Room> rooms)
    {
        var lastRoom = rooms[^1];

        return Run.Rehydrate(
            id: RunId.New(),
            playerId: Guid.NewGuid(),
            seed: seed,
            generatorVersion: generatorVersion,
            markovMatrixVersion: markovMatrixVersion,
            status: RunStatus.Active,
            currentRoomId: lastRoom.Id,
            activeCombatId: null,
            pendingRewardOfferId: null,
            maxHp: 100,
            currentHp: 100,
            attack: 10,
            defense: 10,
            speed: 10,
            focus: 10,
            startedAt: DateTimeOffset.UtcNow,
            endedAt: null,
            savedAt: null,
            currentRoomIndex: rooms.Count - 1,
            rooms: rooms,
            memoryFragments: [],
            activePalaceLaws: [],
            preSuspendStatus: null,
            snapshot: null,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
    }

    [Fact]
    public async Task PreviewUpcomingRoomNames_ShouldMatchActualSequentialGeneration()
    {
        var catalogGateway = CreateWorldGraphGateway();
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator(catalogGateway);

        var initialRoom = await generator.GenerateInitialRoomAsync("seed-preview");
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-preview",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var preview = await generator.PreviewUpcomingRoomNamesAsync(run);

        // 9 rooms remain in the floor (10 rooms/floor, already sitting on room 0).
        preview.Should().HaveCount(9);

        var rooms = new List<Room> { initialRoom };
        var actualKeys = new List<string?>();

        for (var i = 0; i < 9; i++)
        {
            var runAtDepth = RehydrateRunAt("seed-preview", generator.GeneratorVersion, generator.MarkovMatrixVersion, rooms);
            var nextRoom = await generator.GenerateNextRoomAsync(runAtDepth);

            actualKeys.Add(nextRoom.CatalogBinding?.Key);
            rooms.Add(nextRoom);
        }

        preview.Select(p => p.Key).Should().BeEquivalentTo(actualKeys, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task PreviewUpcomingRoomNames_ShouldReturnDisplayNamesMatchingTheCatalog()
    {
        var catalogGateway = CreateWorldGraphGateway();
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator(catalogGateway);

        var initialRoom = await generator.GenerateInitialRoomAsync("seed-preview-names");
        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-preview-names",
            generator.GeneratorVersion,
            generator.MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var preview = await generator.PreviewUpcomingRoomNamesAsync(run);

        preview.Select(p => p.Key).Should().Equal(
            "room.a", "room.b", "room.c", "room.halldentree", "room.a", "room.b", "room.c", "room.halldentree", "room.a");
        preview.First().DisplayName.Should().Be("Salle room.a");
    }

    [Fact]
    public async Task PreviewUpcomingRoomNames_ShouldReturnEmpty_WhenAlreadyOnTheLastRoomOfTheFloor()
    {
        var generator = TestGeneratorFactory.CreateDeterministicRunGenerator();
        var initialRoom = await generator.GenerateInitialRoomAsync("seed-preview-last");

        var rooms = new List<Room> { initialRoom };

        for (var i = 0; i < 9; i++)
        {
            var runAtDepth = RehydrateRunAt("seed-preview-last", generator.GeneratorVersion, generator.MarkovMatrixVersion, rooms);
            var nextRoom = await generator.GenerateNextRoomAsync(runAtDepth);
            rooms.Add(nextRoom);
        }

        var run = RehydrateRunAt("seed-preview-last", generator.GeneratorVersion, generator.MarkovMatrixVersion, rooms);
        run.CurrentRoomIndex.Should().Be(9, because: "the floor is 10 rooms long (0..9).");

        var preview = await generator.PreviewUpcomingRoomNamesAsync(run);

        preview.Should().BeEmpty();
    }
}
