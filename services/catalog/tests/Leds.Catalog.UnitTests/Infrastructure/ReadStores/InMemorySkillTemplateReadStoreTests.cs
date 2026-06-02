using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemorySkillTemplateReadStoreTests
{
    [Fact]
    public async Task ListActiveAsync_ShouldReturnActiveSkillTemplates()
    {
        var readStore = new InMemorySkillTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(template => template.IsActive);
    }

    [Fact]
    public async Task ListActiveAsync_ShouldContainExpectedSeededTemplates()
    {
        var readStore = new InMemorySkillTemplateReadStore();

        var templates = await readStore.ListActiveAsync(CancellationToken.None);

        templates.Select(template => template.Key.Value)
            .Should()
            .BeEquivalentTo(
                "skill-shadow-bite",
                "skill-memory-mend");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyExists()
    {
        var readStore = new InMemorySkillTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "skill-shadow-bite",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("skill-shadow-bite");
        template.Name.Value.Should().Be("Morsure d’Ombre");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTemplate_WhenKeyCaseDiffers()
    {
        var readStore = new InMemorySkillTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "SKILL-SHADOW-BITE",
            CancellationToken.None);

        template.Should().NotBeNull();
        template!.Key.Value.Should().Be("skill-shadow-bite");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        var readStore = new InMemorySkillTemplateReadStore();

        var template = await readStore.GetByKeyAsync(
            "unknown-skill",
            CancellationToken.None);

        template.Should().BeNull();
    }
}