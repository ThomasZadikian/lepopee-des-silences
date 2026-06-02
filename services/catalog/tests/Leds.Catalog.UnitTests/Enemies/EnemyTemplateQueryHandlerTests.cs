using FluentAssertions;
using Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;
using Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;
using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Enemies;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Enemies;

public sealed class EnemyTemplateQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnTemplate_WhenTemplateExists()
    {
        var template = CreateEnemyTemplate();

        var readStore = Substitute.For<IEnemyTemplateReadStore>();
        readStore.GetByKeyAsync("enemy-shadow-wolf", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnemyTemplate?>(template));

        var handler = new GetEnemyTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEnemyTemplateByKeyQuery("enemy-shadow-wolf"),
            CancellationToken.None);

        response.Template.Should().NotBeNull();
        response.Template!.Key.Should().Be("enemy-shadow-wolf");
        response.Template.Name.Should().Be("Loup d’Ombre");
        response.Template.Archetype.Should().Be("Trauma");
        response.Template.Element.Should().Be("Shadow");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullTemplate_WhenTemplateDoesNotExist()
    {
        var readStore = Substitute.For<IEnemyTemplateReadStore>();
        readStore.GetByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnemyTemplate?>(null));

        var handler = new GetEnemyTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEnemyTemplateByKeyQuery("unknown"),
            CancellationToken.None);

        response.Template.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedTemplates()
    {
        var template = CreateEnemyTemplate();

        var readStore = Substitute.For<IEnemyTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IEnemyTemplate>>(
                new[] { template }));

        var handler = new ListActiveEnemyTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveEnemyTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();

        dto.Key.Should().Be("enemy-shadow-wolf");
        dto.Name.Should().Be("Loup d’Ombre");
        dto.Status.Should().Be("Active");
    }

    private static EnemyTemplate CreateEnemyTemplate()
    {
        return EnemyTemplate.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Une entité née dans un couloir du Palais.",
            "catalog-0.1.0",
            EnemyArchetype.Trauma,
            CombatElement.Shadow,
            maxHealth: 120,
            strength: 18,
            intelligence: 8,
            speed: 14,
            physicalResistance: 10,
            magicalResistance: 5,
            experienceReward: 25,
            goldReward: 12,
            status: CatalogContentStatus.Active);
    }
}