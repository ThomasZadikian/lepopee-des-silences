using FluentAssertions;
using Leds.Catalog.Application.Rooms.Dtos;
using Leds.Catalog.Application.Rooms.ListActiveRoomDefinitions;
using Leds.Catalog.Application.Rooms.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Rooms;

public sealed class ListActiveRoomDefinitionsQueryHandlerTests
{
    private static RoomDefinitionDto CreateDto(
        string key = "room.halldentree",
        string? worldKey = "palais",
        bool isWorldEntryRoom = true,
        bool triggersStrictChain = false,
        params string[] reachableRoomKeys)
        => new(
            Guid.NewGuid(), key, "Hall d'entrée", "Description", "Narrative",
            "Palais intérieur", "Epic", "Welcome",
            0, 9, 1, "room.canon",
            null, null, null, null, null, null,
            IsUnique: true, IsCulturalEcho: false,
            worldKey, isWorldEntryRoom, triggersStrictChain,
            reachableRoomKeys, "canon-1.0.0", "Active");

    [Fact]
    public async Task Handle_ShouldReturnMappedDefinitions_WhenDefinitionsExist()
    {
        var dto = CreateDto(reachableRoomKeys: ["room.palier", "room.couloirs"]);

        var readStore = Substitute.For<IRoomDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RoomDefinitionDto>>([dto]));

        var handler = new ListActiveRoomDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveRoomDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        var result = response.Definitions.Single();
        result.Key.Should().Be("room.halldentree");
        result.WorldKey.Should().Be("palais");
        result.IsWorldEntryRoom.Should().BeTrue();
        result.ReachableRoomKeys.Should().BeEquivalentTo("room.palier", "room.couloirs");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDefinitionsExist()
    {
        var readStore = Substitute.For<IRoomDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RoomDefinitionDto>>(Array.Empty<RoomDefinitionDto>()));

        var handler = new ListActiveRoomDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveRoomDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnRoomWithNoWorld_WhenLegacyContent()
    {
        var dto = CreateDto(key: "canon.room.pistburg", worldKey: null, isWorldEntryRoom: false);

        var readStore = Substitute.For<IRoomDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RoomDefinitionDto>>([dto]));

        var handler = new ListActiveRoomDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveRoomDefinitionsQuery(), CancellationToken.None);

        var result = response.Definitions.Single();
        result.WorldKey.Should().BeNull();
        result.ReachableRoomKeys.Should().BeEmpty();
    }
}
