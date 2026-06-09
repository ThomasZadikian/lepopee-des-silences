using FluentAssertions;
using Leds.Catalog.Application.Enemies.Definitions.Dtos;
using Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;
using Leds.Catalog.Application.Enemies.Definitions.ListActiveEnemyDefinitions;
using Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;
using Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Enemies.Definitions;

public sealed class EnemyDefinitionQueryHandlerTests
{
    [Fact]
    public async Task ListActive_ShouldReturnMappedDefinitions()
    {
        var definition = CreateEnemyDefinition();

        var readStore = Substitute.For<IEnemyDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(
                new[] { definition }));

        var handler = new ListActiveEnemyDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveEnemyDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();
        dto.Key.Should().Be("enemy.threshold.doubt-fragment");
        dto.Status.Should().Be("Active");
        dto.Archetype.Should().Be("Fragile");
        dto.BaseDifficulty.Should().Be(1);
        dto.MinRiskLevel.Should().Be(1);
        dto.MaxRiskLevel.Should().Be(2);
        dto.CompatibleRoomTypes.Should().BeEquivalentTo("Threshold");
        dto.Tags.Should().BeEquivalentTo("threshold", "doubt");
        dto.SkillKeys.Should().BeEquivalentTo("skill.basic.strike");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnDefinition_WhenExists()
    {
        var definition = CreateEnemyDefinition();

        var readStore = Substitute.For<IEnemyDefinitionReadStore>();
        readStore.GetByKeyAsync("enemy.threshold.doubt-fragment", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnemyDefinition?>(definition));

        var handler = new GetEnemyDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEnemyDefinitionByKeyQuery("enemy.threshold.doubt-fragment"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();
        response.Definition!.Key.Should().Be("enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNull_WhenNotExists()
    {
        var readStore = Substitute.For<IEnemyDefinitionReadStore>();
        readStore.GetByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnemyDefinition?>(null));

        var handler = new GetEnemyDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetEnemyDefinitionByKeyQuery("unknown"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task ListByRoomType_ShouldReturnDefinitions_WhenRoomTypeExists()
    {
        var definition = CreateEnemyDefinition();

        var readStore = Substitute.For<IEnemyDefinitionReadStore>();
        readStore.ListByRoomTypeAsync("Threshold", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(
                new[] { definition }));

        var handler = new ListEnemyDefinitionsByRoomTypeQueryHandler(readStore);

        var response = await handler.Handle(
            new ListEnemyDefinitionsByRoomTypeQuery("Threshold"),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().CompatibleRoomTypes.Should().Contain("Threshold");
    }

    [Fact]
    public async Task ListCompatible_ShouldReturnDefinitions_WhenRoomTypeAndRiskLevelMatch()
    {
        var definition = CreateEnemyDefinition();

        var readStore = Substitute.For<IEnemyDefinitionReadStore>();
        readStore.ListCompatibleAsync("Threshold", 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(
                new[] { definition }));

        var handler = new ListCompatibleEnemyDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListCompatibleEnemyDefinitionsQuery("Threshold", 1),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
    }

    private static EnemyDefinition CreateEnemyDefinition()
    {
        var def = EnemyDefinition.Create(
            "enemy.threshold.doubt-fragment",
            "Fragment de Doute",
            "Une hesitation cristallisee.",
            "1.0.0",
            "Fragile",
            baseDifficulty: 1,
            minRiskLevel: 1,
            maxRiskLevel: 2,
            compatibleRoomTypes: ["Threshold"],
            tags: ["threshold", "doubt"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }
}
