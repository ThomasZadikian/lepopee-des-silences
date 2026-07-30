using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class GridRoomGeneratorTests
{
    private const string Seed = "seed-grid-room-generator-tests";
    private const string GeneratorVersion = "grid-room-layout-1.0.0";

    private static IGridRoomGenerator CreateSut()
    {
        return new GridRoomGenerator(
            new GridRoomLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver(new StubCatalogContentGateway()),
            new HardcodedRoomTypeGenerationProfileProvider());
    }

    [Fact]
    public async Task GenerateRoom_ShouldUseDefaultGridRoomLayoutTemplate()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.LayoutTemplateKey.Should().Be("tactical-default-v1");
        room.LayoutTemplateVersion.Should().Be(GeneratorVersion);
    }

    [Fact]
    public async Task GenerateRoom_ShouldBuildAGridRoom()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Grid.Should().NotBeNull();
        room.Grid!.Width.Should().Be(14);
        room.Grid.Height.Should().Be(10);
        room.Grid.PartyX.Should().Be(0);
        room.Grid.PartyY.Should().Be(5);

        // The budget is no longer the template's constant: it is derived from the cheapest route
        // to the boss on the room actually generated, plus slack. Asserted as the contract that
        // matters (never below the template floor, always a full budget at spawn) rather than as
        // a magic number, which would break whenever the shape or obstacle rolls shift.
        room.Grid.MovementBudget.Should().BeGreaterThanOrEqualTo(42);
        room.Grid.MovementBudgetRemaining.Should().Be(room.Grid.MovementBudget);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    public async Task GenerateRoom_ShouldLeaveBudgetToSpare_BeyondTheCheapestRouteToTheBoss(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var boss = room.Nodes.Single(node => node.IsBoss);
        var route = room.Grid.FindPath(boss.Lane, boss.Row);

        route.Should().NotBeNull("the boss must always be reachable");

        // Reaching the objective must never be all the budget affords — there has to be room to
        // detour into a recess and search it.
        (room.Grid.MovementBudget - route!.Value.Cost)
            .Should().BeGreaterThan(0, "exploring must stay affordable, not just surviving the room");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    public async Task GenerateRoom_ShouldKeepEveryNodeOnReachableFloor(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        foreach (var node in room.Nodes)
        {
            room.Grid.IsFloor(node.Lane, node.Row)
                .Should().BeTrue("a node cannot stand on a hole in the room");
            room.Grid.IsObstacle(node.Lane, node.Row)
                .Should().BeFalse("a node cannot stand inside a wall");
            room.Grid.FindPath(node.Lane, node.Row)
                .Should().NotBeNull("every node must stay reachable from the spawn");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    public async Task GenerateRoom_ShouldHideOnlyLootAndNeverTheBoss(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        foreach (var hidden in room.Nodes.Where(node => node.IsHidden))
        {
            hidden.IsBoss.Should().BeFalse("hiding the room's objective could strand the run");
            hidden.EventType.Should().Be(NodeEventType.Item,
                "a cache rewards a detour with loot, never with an unchosen fight");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    [InlineData(2026)]
    [InlineData(90210)]
    public async Task GenerateRoom_ShouldAlwaysOfferSomewhereToRest(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().Contain(node => node.EventType == NodeEventType.Rest,
            "left to the weighted draw a room can come out with no breather at all, which makes "
            + "attrition a matter of luck rather than of the player's routing choices");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    public async Task GenerateRoom_ShouldPutTheGuaranteedRestOutOfArmsReachOfTheEntrance(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);
        var grid = room.Grid;

        room.Nodes.Where(node => node.EventType == NodeEventType.Rest)
            .Should().Contain(
                node => Math.Abs(node.Lane - grid.StartX) + Math.Abs(node.Row - grid.StartY) > 1,
                "a breather you trip over on the first step costs nothing to reach and so decides nothing");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    public async Task GenerateRoom_ShouldNotCarveAwayTheWholeRoom(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var floorCount = room.Grid.FloorMask.Count(cell => cell);
        var total = room.Grid.Width * room.Grid.Height;

        room.Grid.IsFloor(room.Grid.StartX, room.Grid.StartY).Should().BeTrue();
        floorCount.Should().BeLessThan(total, "a room should not stay a perfect rectangle");
        floorCount.Should().BeGreaterThan(total / 2, "carving must shape the room, not consume it");
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateNodeCountWithinTemplateRange()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.TotalNodeCount.Should().BeInRange(14, 20);
    }

    [Fact]
    public async Task GenerateRoom_ShouldHaveExactlyOneBossNode()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Where(n => n.IsBoss).Should().ContainSingle()
            .Which.EventType.Should().Be(NodeEventType.RoomBoss);
    }

    [Fact]
    public async Task GenerateRoom_ShouldPlaceEveryNodeWithinGridBounds()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().AllSatisfy(n =>
        {
            n.Lane.Should().BeInRange(0, room.Grid!.Width - 1);
            n.Row.Should().BeInRange(0, room.Grid.Height - 1);
        });
    }

    [Fact]
    public async Task GenerateRoom_ShouldNotPlaceAnyNodeOnTheStartingCell()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().NotContain(n => n.Lane == room.Grid!.PartyX && n.Row == room.Grid.PartyY);
    }

    [Fact]
    public async Task GenerateRoom_ShouldNotPlaceTwoNodesOnTheSameCell()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Select(n => (n.Lane, n.Row)).Distinct().Should().HaveCount(room.Nodes.Count);
    }

    [Fact]
    public async Task GenerateRoom_ShouldHaveNoParentReferences()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().AllSatisfy(n => n.ParentNodeIds.Should().BeEmpty());
    }

    [Fact]
    public async Task GenerateRoom_ShouldStartEveryNodeAsAvailable()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().AllSatisfy(n => n.State.Should().Be(NodeState.Available));
    }

    [Fact]
    public async Task GenerateRoom_ShouldPlaceBossWithinMovementBudget()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        var boss = room.Nodes.Single(n => n.IsBoss);
        var distance = Math.Abs(boss.Lane - room.Grid!.PartyX) + Math.Abs(boss.Row - room.Grid.PartyY);

        distance.Should().BeLessThanOrEqualTo(room.Grid.MovementBudget);
    }

    [Fact]
    public async Task GenerateRoom_ShouldBeDeterministicForSameSeed()
    {
        var sut = CreateSut();

        var room1 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));
        var room2 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(42));

        var signature1 = room1.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane)
            .Select(n => (n.Row, n.Lane, n.EventType)).ToArray();
        var signature2 = room2.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Lane)
            .Select(n => (n.Row, n.Lane, n.EventType)).ToArray();

        signature2.Should().Equal(signature1);
    }

    [Fact]
    public async Task GenerateRoom_ShouldPlaceBossAtTheSameCell_RegardlessOfRandomRolls()
    {
        var sut = CreateSut();

        var room1 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(1));
        var room2 = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, new Random(999));

        var boss1 = room1.Nodes.Single(n => n.IsBoss);
        var boss2 = room2.Nodes.Single(n => n.IsBoss);

        (boss1.Lane, boss1.Row).Should().Be((boss2.Lane, boss2.Row),
            "the boss's position only depends on the template shape, not on the seeded random rolls.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1234)]
    [InlineData(987654)]
    public async Task GenerateRoom_ShouldNeverPlaceObstaclesOnStartNodeOrBossCell(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Grid!.IsObstacle(room.Grid.StartX, room.Grid.StartY).Should().BeFalse();

        room.Nodes.Should().AllSatisfy(n =>
            room.Grid.IsObstacle(n.Lane, n.Row).Should().BeFalse());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1234)]
    [InlineData(987654)]
    public async Task GenerateRoom_ShouldKeepEveryNodeAndBossReachableFromStart(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Nodes.Should().AllSatisfy(n =>
            room.Grid!.FindPath(n.Lane, n.Row).Should().NotBeNull(
                $"node at ({n.Lane},{n.Row}) must be reachable from the start — obstacle " +
                "generation is expected to guarantee connectivity."));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public async Task GenerateRoom_ShouldProduceElevation_ThatIsOneLipschitzBetweenNeighbors(int seed)
    {
        var sut = CreateSut();
        var random = new Random(seed);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);
        var grid = room.Grid!;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (x + 1 < grid.Width)
                {
                    Math.Abs(grid.ElevationAt(x, y) - grid.ElevationAt(x + 1, y)).Should().BeLessThanOrEqualTo(1);
                }

                if (y + 1 < grid.Height)
                {
                    Math.Abs(grid.ElevationAt(x, y) - grid.ElevationAt(x, y + 1)).Should().BeLessThanOrEqualTo(1);
                }
            }
        }
    }

    [Fact]
    public async Task GenerateRoom_ShouldActuallyPopulateElevationAndObstacles_NotJustDefaultToFlat()
    {
        // Canary against a regression where the generator silently stops threading real terrain
        // through to Room.Create and falls back to RoomGrid.CreateInitial's flat/no-obstacle
        // defaults — those defaults exist for hand-built test rooms, not for generated ones.
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.Grid!.Elevation.Should().Contain(level => level > 0,
            "the default template's board is large enough that a cone-falloff heightmap should raise at least one cell.");
        room.Grid.Obstacles.Should().NotBeEmpty(
            "the default template's board is large enough that some obstacles should survive the connectivity check.");
    }

    [Fact]
    public async Task GenerateRoom_ShouldKeepObstacleDensityWithinExpectedBand()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);
        var grid = room.Grid!;

        var totalCells = grid.Width * grid.Height;

        // Loose band, not the exact 15% knob — connectivity checks can and do discard some
        // candidates, so this only guards against a gross regression (e.g. no obstacles at all,
        // or so many the board becomes unplayable).
        grid.Obstacles.Count.Should().BeInRange(0, (int)(totalCells * 0.3));
    }
}
