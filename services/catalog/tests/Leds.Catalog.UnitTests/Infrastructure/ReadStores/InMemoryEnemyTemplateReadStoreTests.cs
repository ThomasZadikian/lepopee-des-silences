using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryEnemyTemplateReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveEnemyTemplates()
    {
        var readStore = new InMemoryEnemyTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(template => template.IsActive);
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainExpectedSeededTemplates()
    {
        var readStore = new InMemoryEnemyTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Select(template => template.Key.Value)
            .Should()
            .BeEquivalentTo("enemy-shadow-wolf");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyExists()
    {
        var readStore = new InMemoryEnemyTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "enemy-shadow-wolf",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("enemy-shadow-wolf");
        template.Name.Value.Should().Be("Loup d’Ombre");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyCaseDiffers()
    {
        var readStore = new InMemoryEnemyTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "ENEMY-SHADOW-WOLF",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("enemy-shadow-wolf");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemoryEnemyTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "unknown-enemy",
            CancellationToken.None);

        template.Should().BeNull();
    }
}