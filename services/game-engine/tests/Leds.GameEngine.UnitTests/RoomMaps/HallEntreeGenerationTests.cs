using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class HallEntreeGenerationTests
{
    private const string Seed = "seed-hall-entree-tests";
    private const string GeneratorVersion = "grid-room-layout-1.0.0";
    private const string CatalogRoomKey = "room.halldentree";

    private static IGridRoomGenerator CreateSut()
    {
        return new GridRoomGenerator(
            new GridRoomLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver(new StubCatalogContentGateway()),
            new HardcodedRoomTypeGenerationProfileProvider(),
            new HardcodedRoomStructuralProfileProvider(),
            new HardcodedLocalRuleProvider());
    }

    private static Task<Room> GenerateHallAsync(int seed = 42) =>
        CreateSut().GenerateAsync(
            Seed, GeneratorVersion, roomDepth: 0, RoomType.Memory, new Random(seed),
            catalogRoomKey: CatalogRoomKey);

    [Fact]
    public async Task GenerateHall_ShouldUseTheAuthoredLayoutTemplate()
    {
        var room = await GenerateHallAsync();

        room.LayoutTemplateKey.Should().Be("room.halldentree-v1");
        room.Grid.Width.Should().Be(26);
        room.Grid.Height.Should().Be(18);
        room.Grid.PartyX.Should().Be(12);
        room.Grid.PartyY.Should().Be(15);
    }

    [Fact]
    public async Task GenerateHall_ShouldExposeExactlyFourPillars_AsNonBlockingLandmarks()
    {
        var room = await GenerateHallAsync();

        HallEntreeLayout.Pillars.Should().HaveCount(4);
        // Pillars are decor, not collision (the reference implementation has no obstacle layer
        // at all) — one of them sits directly beside the west salon's only door, so treating them
        // as RoomGrid obstacles would make that door permanently unreachable.
        room.Grid.Obstacles.Should().BeEmpty();
        foreach (var (x, y) in HallEntreeLayout.Pillars)
        {
            room.Grid.IsWalkable(x, y).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GenerateHall_ShouldPaintTheTapisAsACarpetSurfaceOverride()
    {
        var room = await GenerateHallAsync();

        room.Grid.SurfaceOverrides.Should().HaveCount(HallEntreeLayout.TapisCells.Count);
        room.Grid.SurfaceAt(12, 10).Should().Be(HallEntreeLayout.TapisSurfaceKey);
        // Off the tapis band entirely — must carry no override at all.
        room.Grid.SurfaceAt(2, 10).Should().BeNull();
    }

    [Fact]
    public async Task GenerateHall_ShouldPlaceDecorForPillarsAndAuthoredSectorProps()
    {
        var room = await GenerateHallAsync();

        foreach (var (x, y) in HallEntreeLayout.Pillars)
        {
            room.Grid.DecorAt(x, y).Should().Be(HallEntreeLayout.PillarDecorKey);
        }

        foreach (var (x, y, key) in HallEntreeLayout.SectorDecor)
        {
            room.Grid.DecorAt(x, y).Should().Be(key);
        }

        room.Grid.DecorPlacements.Should().HaveCount(
            HallEntreeLayout.Pillars.Count + HallEntreeLayout.SectorDecor.Count);
    }

    [Fact]
    public async Task GenerateHall_DecorPlacementsShouldNeverCollideWithNodesOrRoomNpcs()
    {
        var room = await GenerateHallAsync();

        var occupied = room.Nodes.Select(n => (X: n.Lane, Y: n.Row))
            .Concat(room.RoomNpcs.Select(npc => (npc.X, npc.Y)))
            .ToHashSet();

        foreach (var cell in room.Grid.DecorPlacements.Keys)
        {
            occupied.Should().NotContain(cell, $"decor at {cell} must not sit under a node or an NPC");
        }
    }

    [Fact]
    public async Task GenerateHall_ShouldCarveTheStaircaseElevation()
    {
        var room = await GenerateHallAsync();

        for (var x = 9; x <= 15; x++)
        {
            room.Grid.ElevationAt(x, 3).Should().Be(1);
            room.Grid.ElevationAt(x, 2).Should().Be(2);
            room.Grid.ElevationAt(x, 1).Should().Be(3);
        }

        // Off the staircase columns, the room stays flat.
        room.Grid.ElevationAt(2, 2).Should().Be(0);
    }

    [Fact]
    public async Task GenerateHall_ShouldPunchDoorsAtEverySalonAlcoveAndThreshold()
    {
        var room = await GenerateHallAsync();

        room.Grid.Doors.Should().BeEquivalentTo(new[]
        {
            (8, 6), (17, 6), (7, 13), (18, 13), (1, 10), (24, 10), (12, 16),
        });
    }

    [Fact]
    public async Task GenerateHall_ShouldKeepTheSalonAndAlcoveInteriorsWalkable()
    {
        var room = await GenerateHallAsync();

        // Doors themselves must be floor and open (not obstacles), and the room the door serves
        // must be reachable through it.
        room.Grid.IsWalkable(8, 6).Should().BeTrue();
        room.Grid.IsWalkable(4, 6).Should().BeTrue("west salon interior");
        room.Grid.FindPath(4, 6).Should().NotBeNull("the west salon must be reachable from the entrance");
    }

    [Fact]
    public async Task GenerateHall_ShouldNotInventABoss_WhenCatalogDeclaresNone()
    {
        var room = await GenerateHallAsync();

        room.BossProfile.Should().BeNull();
        room.Nodes.Should().NotContain(n => n.IsBoss);
        room.Nodes.Should().OnlyContain(n => n.EventType == Leds.GameEngine.Domain.Nodes.NodeEventType.Item);
    }

    [Fact]
    public async Task GenerateHall_ShouldPopulateTheAuthoredCastRoster()
    {
        var room = await GenerateHallAsync();

        room.RoomNpcs.Should().HaveCount(HallEntreeCasting.Roster.Count);

        var majordome = room.RoomNpcs.Should().ContainSingle(n => n.CatalogNpcKey == "npc.majordome").Subject;
        majordome.X.Should().Be(12);
        majordome.Y.Should().Be(13);
        majordome.Behavior.Should().Be(NpcBehaviorArchetype.Hunter);
        majordome.Awareness.Should().Be(NpcAwarenessState.Unaware);

        room.RoomNpcs.Where(n => n.CatalogNpcKey is "npc.habitant#0" or "npc.habitant#1")
            .Should().HaveCount(2)
            .And.OnlyContain(n => n.AwarenessRadius == 0, "ambient habitants must never notice the party on their own");
    }

    [Fact]
    public async Task GenerateHall_ShouldExposeTheInitiallyVisibleCastToTheClient()
    {
        var room = await GenerateHallAsync();

        var dto = RoomDto.FromDomain(room);

        dto.RoomNpcs.Should().Contain(npc => npc.CatalogNpcKey == "npc.majordome");
        dto.RoomNpcs.Should().Contain(npc => npc.CatalogNpcKey == "npc.veilleur-tapis");
    }

    [Fact]
    public async Task GenerateHall_ShouldPlaceEveryRoomNpcOnWalkableFloor()
    {
        var room = await GenerateHallAsync();

        foreach (var npc in room.RoomNpcs)
        {
            room.Grid.IsWalkable(npc.X, npc.Y).Should().BeTrue(
                $"RoomNpc '{npc.CatalogNpcKey}' at ({npc.X},{npc.Y}) must stand on walkable floor");
        }
    }

    [Fact]
    public async Task MovePartyInHall_ShouldKeepTheMajordomeOffThePartyCell()
    {
        var room = await GenerateHallAsync();

        room.MoveParty(12, 12);

        var majordome = room.RoomNpcs.Single(npc => npc.CatalogNpcKey == "npc.majordome");
        (majordome.X, majordome.Y)
            .Should().NotBe((room.Grid.PartyX, room.Grid.PartyY));
    }

    [Fact]
    public async Task GenerateHall_ShouldKeepOnlyItsExistingAuthoredScaffoldingNodes()
    {
        var room = await GenerateHallAsync();

        room.ContentNodeCount.Should().Be(5, "the Hall must not synthesize a boss or a replacement event");
    }

    [Fact]
    public async Task GenerateHall_ShouldBeAcceptedAsARunsOpeningRoom()
    {
        var room = await GenerateHallAsync();

        var act = () => Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: Seed,
            generatorVersion: GeneratorVersion,
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        act.Should().NotThrow("the Hall must be able to open a run (SFD §I: entree canonique de la run)");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(1337)]
    public async Task GenerateHall_ShouldBeIdenticalRegardlessOfRandomSeed(int seed)
    {
        // The Hall's geometry and casting are fully authored — no random draw feeds them — so
        // the room must come out identical no matter what Random instance drives generation.
        var room = await GenerateHallAsync(seed);

        room.Grid.Obstacles.Should().BeEmpty();
        room.RoomNpcs.Select(n => (n.CatalogNpcKey, n.X, n.Y))
            .Should().BeEquivalentTo(HallEntreeCasting.Roster.Select(e => (e.CatalogNpcKey, e.X, e.Y)));
        room.Grid.SurfaceOverrides.Should().HaveCount(HallEntreeLayout.TapisCells.Count);
        room.Grid.DecorPlacements.Should().HaveCount(
            HallEntreeLayout.Pillars.Count + HallEntreeLayout.SectorDecor.Count);
    }
}
