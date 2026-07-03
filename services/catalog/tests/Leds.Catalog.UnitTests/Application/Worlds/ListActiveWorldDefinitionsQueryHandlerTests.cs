using FluentAssertions;
using Leds.Catalog.Application.Worlds.Dtos;
using Leds.Catalog.Application.Worlds.ListActiveWorldDefinitions;
using Leds.Catalog.Application.Worlds.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Worlds;

public sealed class ListActiveWorldDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedDefinitions_WhenDefinitionsExist()
    {
        var dto = new WorldDefinitionDto(Guid.NewGuid(), "palais", "Palais", "room.halldentree", "canon-1.0.0", "Active");

        var readStore = Substitute.For<IWorldDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<WorldDefinitionDto>>([dto]));

        var handler = new ListActiveWorldDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveWorldDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        var result = response.Definitions.Single();
        result.Key.Should().Be("palais");
        result.EntryRoomKey.Should().Be("room.halldentree");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDefinitionsExist()
    {
        var readStore = Substitute.For<IWorldDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<WorldDefinitionDto>>(Array.Empty<WorldDefinitionDto>()));

        var handler = new ListActiveWorldDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveWorldDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }
}
