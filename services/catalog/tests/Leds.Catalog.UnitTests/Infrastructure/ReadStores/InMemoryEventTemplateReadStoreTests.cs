using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryEventTemplateReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveEventTemplates()
    {
        var readStore = new InMemoryEventTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(template => template.IsActive);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyExists()
    {
        var readStore = new InMemoryEventTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "event-memory-threshold-v1",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("event-memory-threshold-v1");
        template.Name.Value.Should().Be("Mémoire du seuil");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyCaseDiffers()
    {
        var readStore = new InMemoryEventTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "EVENT-MEMORY-THRESHOLD-V1",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("event-memory-threshold-v1");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemoryEventTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "unknown-event",
            CancellationToken.None);

        template.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainExpectedSeededTemplates()
    {
        var readStore = new InMemoryEventTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Select(template => template.Key.Value)
            .Should()
            .BeEquivalentTo(
                "event-memory-threshold-v1",
                "event-law-silence-v1",
                "event-combat-shadow-v1");
    }
}