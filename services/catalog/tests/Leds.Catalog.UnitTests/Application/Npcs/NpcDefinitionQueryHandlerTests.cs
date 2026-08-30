using FluentAssertions;
using Leds.Catalog.Application.Npcs.Definitions.ListActiveNpcDefinitions;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Npcs;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Npcs;

public sealed class NpcDefinitionQueryHandlerTests
{
    [Fact]
    public async Task ListActive_ShouldReturnMappedDefinitions()
    {
        var definition = CreateNpcDefinition();

        var readStore = Substitute.For<INpcDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<INpcDefinition>>(
                new[] { definition }));

        var handler = new ListActiveNpcDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveNpcDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();

        dto.Key.Should().Be("npc-neutral-traveler");
        dto.Name.Should().Be("Voyageur neutre");
        dto.Status.Should().Be("Active");
        dto.Tags.Should().BeEquivalentTo("npc", "generic");
        dto.CompatiblePalaceRoomStates.Should().BeEmpty();
        dto.CompatibleRoomClimates.Should().BeEmpty();
        dto.MinDepth.Should().BeNull();
        dto.MaxDepth.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnEmpty_WhenNoDefinitions()
    {
        var readStore = Substitute.For<INpcDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<INpcDefinition>>(
                Array.Empty<INpcDefinition>()));

        var handler = new ListActiveNpcDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveNpcDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ListActive_ShouldMapPalaceRoomStatesCorrectly()
    {
        var definition = CreateNpcDefinitionWithStateConstraints();

        var readStore = Substitute.For<INpcDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<INpcDefinition>>(
                new[] { definition }));

        var handler = new ListActiveNpcDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveNpcDefinitionsQuery(),
            CancellationToken.None);

        var dto = response.Definitions.Single();
        dto.CompatiblePalaceRoomStates.Should().BeEquivalentTo("Silent");
        dto.CompatibleRoomClimates.Should().BeEquivalentTo("Rain");
        dto.MinDepth.Should().Be(1);
        dto.MaxDepth.Should().Be(5);
    }

    private static INpcDefinition CreateNpcDefinition()
    {
        var definition = NpcDefinition.Create(
            "npc-neutral-traveler",
            "Voyageur neutre",
            "Un voyageur sans attache.",
            "alpha-0.8.1",
            tags: ["npc", "generic"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null);

        definition.Activate();
        return definition;
    }

    private static INpcDefinition CreateNpcDefinitionWithStateConstraints()
    {
        var definition = NpcDefinition.Create(
            "npc-silent-witness",
            "Témoin silencieux",
            null,
            "alpha-0.8.1",
            tags: null,
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: ["Silent"],
            compatibleRoomClimates: ["Rain"],
            minDepth: 1,
            maxDepth: 5);

        definition.Activate();
        return definition;
    }
}
