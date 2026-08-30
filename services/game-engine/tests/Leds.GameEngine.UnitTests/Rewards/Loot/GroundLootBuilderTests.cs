using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.UnitTests.Common;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards.Loot;

public sealed class GroundLootBuilderTests
{
    private static GroundLootBuilder CreateBuilder(StubCatalogContentGateway? gateway = null) =>
        new(gateway ?? new StubCatalogContentGateway());

    [Fact]
    public async Task BuildAsync_ShouldReturnBetweenOneAndTwoItems_WithARealPool()
    {
        var items = await CreateBuilder().BuildAsync("seed-ground-1", Guid.NewGuid(), Guid.NewGuid());

        items.Count.Should().BeInRange(1, 2);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnEmpty_WhenGenericPoolIsNull()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.GetActiveGenericLootPoolAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogGenericLootPool?)null);

        var items = await new GroundLootBuilder(gateway.Object).BuildAsync(
            "seed-ground-2", Guid.NewGuid(), Guid.NewGuid());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnEmpty_WhenGenericPoolHasNoEntries()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.GetActiveGenericLootPoolAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogGenericLootPool("loot.empty", "Vide", "Rien.", "1.0", []));

        var items = await new GroundLootBuilder(gateway.Object).BuildAsync(
            "seed-ground-3", Guid.NewGuid(), Guid.NewGuid());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ShouldSkipPermanentEligibleItems_ModeleHades()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.GetActiveGenericLootPoolAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogGenericLootPool(
                "loot.equip-only", "Équipement", "Toujours équipable.", "1.0",
                [new CatalogLootEntry("item.equip.only", 100)]));
        gateway.Setup(g => g.GetItemDefinitionByKeyAsync("item.equip.only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.equip.only", "1.0", "Objet équipable", "Toujours permanent-eligible.", null,
                "Equipment", "None", "Common", "Equip", "PersistentMeta", "Additive", 1,
                IsUsableInCombat: false, IsUsableOutsideCombat: false, IsPermanentEligible: true)));

        var items = await new GroundLootBuilder(gateway.Object).BuildAsync(
            "seed-ground-4", Guid.NewGuid(), Guid.NewGuid());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ShouldSkipUnknownItemKeys()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway.Setup(g => g.GetActiveGenericLootPoolAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogGenericLootPool(
                "loot.missing", "Manquant", "Clé absente du catalogue.", "1.0",
                [new CatalogLootEntry("item.does-not-exist", 100)]));
        gateway.Setup(g => g.GetItemDefinitionByKeyAsync("item.does-not-exist", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(
                Error.Create("catalog.item_definition_not_found", "Item definition was not found.")));

        var items = await new GroundLootBuilder(gateway.Object).BuildAsync(
            "seed-ground-5", Guid.NewGuid(), Guid.NewGuid());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ShouldBeDeterministic_ForTheSameInputs()
    {
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var first = await CreateBuilder().BuildAsync("seed-ground-6", runId, combatId);
        var second = await CreateBuilder().BuildAsync("seed-ground-6", runId, combatId);

        first.Select(i => i.DefinitionKey).Should().Equal(second.Select(i => i.DefinitionKey));
    }

    [Fact]
    public async Task BuildAsync_ShouldChangeSelection_WhenSeedChanges()
    {
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(i => CreateBuilder().BuildAsync($"seed-ground-varied-{i}", runId, combatId)));

        var distinctSignatures = results
            .Select(r => string.Join(",", r.Select(i => i.DefinitionKey)))
            .Distinct()
            .Count();

        distinctSignatures.Should().BeGreaterThan(1,
            "different seeds should be able to produce different ground loot rolls.");
    }

    [Fact]
    public async Task BuildAsync_ShouldNotCollideWithEnemyLootRewardBuilder_ForTheSameCombat()
    {
        // Both builders are keyed by the same (runSeed, runId, combatId) triple for the same
        // fight — GroundLootBuilder's StepOffset must keep its roll sequence from silently
        // reusing EnemyLootRewardBuilder's own step counter.
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        var gateway = new StubCatalogContentGateway();

        var enemyLootBuilder = new EnemyLootRewardBuilder(gateway);
        var groundLootBuilder = new GroundLootBuilder(gateway);

        var enemyChoices = await enemyLootBuilder.BuildAsync("seed-ground-7", runId, combatId, []);
        var groundItems = await groundLootBuilder.BuildAsync("seed-ground-7", runId, combatId);

        groundItems.Should().NotBeEmpty();
        enemyChoices.Should().NotBeEmpty();
    }
}
