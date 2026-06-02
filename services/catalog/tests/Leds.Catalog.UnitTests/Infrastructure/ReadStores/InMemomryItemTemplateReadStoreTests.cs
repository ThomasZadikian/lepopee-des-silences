using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryItemTemplateReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveItemTemplates()
    {
        var readStore = new InMemoryItemTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(template => template.IsActive);
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainExpectedSeededTemplates()
    {
        var readStore = new InMemoryItemTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Select(template => template.Key.Value)
            .Should()
            .BeEquivalentTo(
                "item-memory-potion",
                "item-silent-relic");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyExists()
    {
        var readStore = new InMemoryItemTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "item-memory-potion",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("item-memory-potion");
        template.Name.Value.Should().Be("Potion de Mémoire");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyCaseDiffers()
    {
        var readStore = new InMemoryItemTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "ITEM-MEMORY-POTION",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("item-memory-potion");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemoryItemTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "unknown-item",
            CancellationToken.None);

        template.Should().BeNull();
    }
}