using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryNpcDefinitionReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveNpcDefinitions()
    {
        var readStore = new InMemoryNpcDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(definition => definition.IsActive);
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainAllExpectedSeededNpcs()
    {
        var readStore = new InMemoryNpcDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Select(d => d.Key.Value)
            .Should()
            .BeEquivalentTo(
                "npc-neutral-traveler",
                "npc-silent-witness",
                "npc-desert-exile",
                "npc-rage-beggar",
                "npc-rain-memory-keeper");
    }

    [Fact]
    public async Task ListActiveAsync_ShouldReturnNpcWithCorrectStateConstraints()
    {
        var readStore = new InMemoryNpcDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        var silentWitness = definitions.Single(d => d.Key.Value == "npc-silent-witness");
        silentWitness.CompatiblePalaceRoomStates.Should().BeEquivalentTo("Silent");
        silentWitness.CompatibleRoomClimates.Should().BeEmpty();
        silentWitness.CompatibleRoomTypes.Should().BeEmpty();

        var desertExile = definitions.Single(d => d.Key.Value == "npc-desert-exile");
        desertExile.CompatiblePalaceRoomStates.Should().BeEmpty();
        desertExile.CompatibleRoomClimates.Should().BeEquivalentTo("Heatwave");

        var rageBeggar = definitions.Single(d => d.Key.Value == "npc-rage-beggar");
        rageBeggar.CompatiblePalaceRoomStates.Should().BeEquivalentTo("Enraged");

        var rainMemoryKeeper = definitions.Single(d => d.Key.Value == "npc-rain-memory-keeper");
        rainMemoryKeeper.CompatibleRoomClimates.Should().BeEquivalentTo("Rain");
    }

    [Fact]
    public async Task ListActiveAsync_ShouldReturnNpcNeutralTraveler_WithNoConstraints()
    {
        var readStore = new InMemoryNpcDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        var traveler = definitions.Single(d => d.Key.Value == "npc-neutral-traveler");
        traveler.CompatibleRoomTypes.Should().BeEmpty();
        traveler.CompatiblePalaceRoomStates.Should().BeEmpty();
        traveler.CompatibleRoomClimates.Should().BeEmpty();
        traveler.MinDepth.Should().BeNull();
        traveler.MaxDepth.Should().BeNull();
    }
}
