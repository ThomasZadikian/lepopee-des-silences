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
        room.Grid!.Width.Should().Be(10);
        room.Grid.Height.Should().Be(8);
        room.Grid.MovementBudget.Should().Be(26);
        room.Grid.MovementBudgetRemaining.Should().Be(26);
        room.Grid.PartyX.Should().Be(0);
        room.Grid.PartyY.Should().Be(4);
    }

    [Fact]
    public async Task GenerateRoom_ShouldCreateNodeCountWithinTemplateRange()
    {
        var sut = CreateSut();
        var random = new Random(42);

        var room = await sut.GenerateAsync(Seed, GeneratorVersion, roomDepth: 0, RoomType.Threshold, random);

        room.TotalNodeCount.Should().BeInRange(10, 14);
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
}
