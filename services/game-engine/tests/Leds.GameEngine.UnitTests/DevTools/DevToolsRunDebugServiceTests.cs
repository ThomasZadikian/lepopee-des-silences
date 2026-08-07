using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.DevTools;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.DevTools;

public sealed class DevToolsRunDebugServiceTests
{
    [Fact]
    public async Task KillAllCurrentCombatEnemies_ShouldDefeatEnemiesOnly()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 2);
        var service = await CreateServiceAsync(setup.Run);

        var result = await service.KillAllCurrentCombatEnemiesAsync(setup.Run.Id.Value);

        result.Combat.Enemies.Should().OnlyContain(enemy => enemy.Combatant.Status == "Defeated" && enemy.Combatant.CurrentVitality == 0);
        result.Combat.Allies.Should().OnlyContain(ally => ally.Combatant.Status == "Active" && ally.Combatant.CurrentVitality > 0);
        result.Combat.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task KillCurrentCombatEnemy_ShouldDefeatOnlyTargetEnemy()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 2);
        var service = await CreateServiceAsync(setup.Run);
        var targetEnemy = setup.Combat.Enemies.First();

        var result = await service.KillCurrentCombatEnemyAsync(setup.Run.Id.Value, targetEnemy.Id.Value);

        result.Combat.Enemies.Single(enemy => enemy.Combatant.Id == targetEnemy.Id.Value).Combatant.Status.Should().Be("Defeated");
        result.Combat.Enemies.Single(enemy => enemy.Combatant.Id != targetEnemy.Id.Value).Combatant.Status.Should().Be("Active");
        result.Combat.Status.Should().Be("Active");
    }

    [Fact]
    public async Task SetCurrentCombatantVitals_ShouldUpdateVitalityAndGuard()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 1);
        var service = await CreateServiceAsync(setup.Run);
        var ally = setup.Combat.Allies.Single();

        var result = await service.SetCurrentCombatantVitalsAsync(
            setup.Run.Id.Value,
            ally.Id.Value,
            vitality: 1,
            guard: 99);

        var updatedAlly = result.Combat.Allies.Single();
        updatedAlly.Combatant.CurrentVitality.Should().Be(1);
        updatedAlly.Combatant.Guard.Should().Be(99);
        updatedAlly.Combatant.Status.Should().Be("Active");
    }

    [Fact]
    public async Task SetCurrentCombatantVitals_ShouldRejectNegativeValues()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 1);
        var service = await CreateServiceAsync(setup.Run);
        var ally = setup.Combat.Allies.Single();

        var act = () => service.SetCurrentCombatantVitalsAsync(
            setup.Run.Id.Value,
            ally.Id.Value,
            vitality: 1,
            guard: -1);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task AddDebugItem_ShouldGrantCatalogItemToTheRunInventory()
    {
        var run = TestGameEngineFactory.CreateRun();
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(gateway => gateway.GetItemDefinitionByKeyAsync("item.the-seuil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                Key: "item.the-seuil",
                Version: "1",
                DisplayName: "Thé du seuil",
                Description: "Restaure 25% de Vitalité maximale.",
                NarrativeText: null,
                Category: "Consumable",
                FlavorTag: "Consumable",
                Rarity: "Common",
                UsageMode: "UseAnywhere",
                Lifecycle: "RuntimeRunOnly",
                StackPolicy: "Additive",
                MaxStack: 20,
                IsUsableInCombat: true,
                IsUsableOutsideCombat: true,
                EffectSetKey: null,
                EffectValue: 25,
                EffectRunType: "HealPercent")));
        var service = await CreateServiceAsync(run, catalogGateway.Object);

        var result = await service.AddDebugItemAsync(run.Id.Value, "item.the-seuil", quantity: 3);

        var grantedItem = result.Run.InventoryItems.Should().ContainSingle().Subject;
        grantedItem.DefinitionKey.Should().Be("item.the-seuil");
        grantedItem.DisplayName.Should().Be("Thé du seuil");
        grantedItem.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddDebugItem_ShouldRejectUnknownItemKey()
    {
        var run = TestGameEngineFactory.CreateRun();
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(gateway => gateway.GetItemDefinitionByKeyAsync("item.nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(
                Error.Create("item.not_found", "Item not found.")));
        var service = await CreateServiceAsync(run, catalogGateway.Object);

        var act = () => service.AddDebugItemAsync(run.Id.Value, "item.nonexistent", quantity: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddDebugItem_ShouldRejectQuantityOutOfRange()
    {
        var run = TestGameEngineFactory.CreateRun();
        var service = await CreateServiceAsync(run);

        var act = () => service.AddDebugItemAsync(run.Id.Value, "item.the-seuil", quantity: 0);

        await act.Should().ThrowAsync<DomainException>();
    }

    private static async Task<DevToolsRunDebugService> CreateServiceAsync(
        Run run, ICatalogContentGateway? catalogContentGateway = null)
    {
        var runRepository = new StubRunRepository();
        await runRepository.AddAsync(run, CancellationToken.None);

        return new DevToolsRunDebugService(
            runRepository,
            Mock.Of<IRunGenerator>(),
            catalogContentGateway ?? Mock.Of<ICatalogContentGateway>(),
            Mock.Of<ICombatResolutionService>(),
            Mock.Of<IRewardOfferRepository>());
    }

    private static (Run Run, TacticalCombat Combat) CreateRunWithActiveCombat(int enemyCount)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemies = Enumerable.Range(0, enemyCount)
            .Select(index => Combatant.CreateEnemy($"enemy.{index}", $"Enemy {index}", "Guard", 80, []))
            .ToArray();

        var (run, combat) = TestTacticalCombatHelper.CreateRunWithCombat(
            runWithNode.Run, [ally], enemies);

        return (run, combat);
    }
}
