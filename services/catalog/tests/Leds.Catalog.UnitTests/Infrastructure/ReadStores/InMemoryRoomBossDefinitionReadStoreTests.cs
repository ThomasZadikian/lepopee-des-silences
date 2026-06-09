using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryRoomBossDefinitionReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveRoomBossDefinitions()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(definition => definition.IsActive);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnDefinition_WhenKeyExists()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "boss.threshold.warden",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.Key.Value.Should().Be("boss.threshold.warden");
        definition.Name.Value.Should().Be("Warden of the Threshold");
        definition.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnDefinition_WhenKeyCaseDiffers()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "BOSS.THRESHOLD.WARDEN",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.Key.Value.Should().Be("boss.threshold.warden");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "unknown-boss",
            CancellationToken.None);

        definition.Should().BeNull();
    }

    [Fact]
    public async Task GetByRoomTypeAsync_ShouldReturnDefinition_WhenRoomTypeExists()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByRoomTypeAsync(
            "Threshold",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.RoomType.Should().Be("Threshold");
        definition.BaseDifficulty.Should().Be(1);
    }

    [Fact]
    public async Task GetByRoomTypeAsync_ShouldReturnDefinition_WhenRoomTypeCaseDiffers()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByRoomTypeAsync(
            "THRESHOLD",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.RoomType.Should().Be("Threshold");
    }

    [Fact]
    public async Task GetByRoomTypeAsync_ShouldReturnNull_WhenRoomTypeDoesNotExist()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definition = await readStore.GetByRoomTypeAsync(
            "Unknown",
            CancellationToken.None);

        definition.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainAllExpectedSeededDefinitions()
    {
        var readStore = new InMemoryRoomBossDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Select(definition => definition.Key.Value)
            .Should()
            .BeEquivalentTo(
                "boss.threshold.warden",
                "boss.forest.rootbound-memory",
                "boss.rupture.fractured-echo",
                "boss.silence.mute-herald",
                "boss.antechamber.last-door",
                "boss.memory.archivist",
                "boss.final.himlit");
    }
}
