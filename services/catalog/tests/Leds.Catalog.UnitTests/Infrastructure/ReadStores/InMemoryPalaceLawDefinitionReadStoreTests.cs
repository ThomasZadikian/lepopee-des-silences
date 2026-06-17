using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryPalaceLawDefinitionReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActivePalaceLawDefinitions()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(definition => definition.IsActive);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnDefinition_WhenKeyExists()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "law-silence-v1",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.Key.Value.Should().Be("law-silence-v1");
        definition.Name.Value.Should().Be("Loi du Silence");
        definition.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnDefinition_WhenKeyCaseDiffers()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "LAW-SILENCE-V1",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.Key.Value.Should().Be("law-silence-v1");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definition = await readStore.GetByKeyAsync(
            "unknown-law",
            CancellationToken.None);

        definition.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainExpectedSeededDefinitions()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definitions = await readStore.ListActiveAsync(CancellationToken.None);

        definitions.Select(definition => definition.Key.Value)
            .Should()
            .BeEquivalentTo(
                "law-silence-v1",
                "law-rupture-v1");
    }

    [Fact]
    public async Task GetDtoByKeyAsync_ShouldExposeEffectDefinitions()
    {
        var readStore = new InMemoryPalaceLawDefinitionReadStore();

        var definition = await readStore.GetDtoByKeyAsync(
            "law-aegis-v1",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.EffectSetKey.Should().Be("effect.law.aegis");
        definition.Effects.Should().ContainSingle(effect =>
            effect.EffectType == "AddStartingGuard" &&
            effect.Value == 8m &&
            effect.Duration == "UntilRunEnds");
    }
}
