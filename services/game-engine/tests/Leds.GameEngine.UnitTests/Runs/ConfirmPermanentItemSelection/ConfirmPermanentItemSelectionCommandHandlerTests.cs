using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ConfirmPermanentItemSelection;

public sealed class ConfirmPermanentItemSelectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldAddPermanentItems_WhenSelectionIsValid()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.TryAddRunItem(CreateItem("item.relic.tome"));

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.relic.tome", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(CreateSnapshot("item.relic.tome", isPermanentEligible: true)));

        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new ConfirmPermanentItemSelectionCommandHandler(
            repository.Object, catalogGateway.Object, playerProfileGateway);

        var response = await handler.Handle(
            new ConfirmPermanentItemSelectionCommand(run.Id.Value, ["item.relic.tome"]),
            CancellationToken.None);

        response.ConfirmedItemDefinitionKeys.Should().Contain("item.relic.tome");
        playerProfileGateway.AddedPermanentItems.Should().ContainSingle(call =>
            call.PlayerId == run.PlayerId &&
            call.SourceRunId == run.Id.Value &&
            call.ItemDefinitionKeys.Contains("item.relic.tome"));
    }

    [Fact]
    public async Task Handle_ShouldCarryOverContainerContent_WhenConfirmedItemIsAFilledContainer()
    {
        var run = TestGameEngineFactory.CreateRun();
        var container = RunItem.Create(
            "item.fiole-cristal", "Fiole de cristal", "",
            RunItemType.Passive, RunItemRarity.Rare, 1, RunItemEffectType.None, 0,
            isContainer: true, containerCapacity: 1);
        var liquid = RunItem.Create(
            "item.larme-de-racine", "Larme de racine", "",
            RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Heal, 12,
            isLiquid: true);
        run.AddRunItem(container);
        run.AddRunItem(liquid);
        run.PourLiquid(container.Id, liquid.Id);

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.fiole-cristal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(CreateSnapshot("item.fiole-cristal", isPermanentEligible: true)));

        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new ConfirmPermanentItemSelectionCommandHandler(
            repository.Object, catalogGateway.Object, playerProfileGateway);

        await handler.Handle(
            new ConfirmPermanentItemSelectionCommand(run.Id.Value, ["item.fiole-cristal"]),
            CancellationToken.None);

        playerProfileGateway.SetPermanentItemContents.Should().ContainSingle(call =>
            call.PlayerId == run.PlayerId &&
            call.ItemDefinitionKey == "item.fiole-cristal" &&
            call.LiquidDefinitionKey == "item.larme-de-racine");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenItemWasNotObtainedDuringTheRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new ConfirmPermanentItemSelectionCommandHandler(
            repository.Object, catalogGateway.Object, playerProfileGateway);

        var act = () => handler.Handle(
            new ConfirmPermanentItemSelectionCommand(run.Id.Value, ["item.never-picked-up"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        playerProfileGateway.AddedPermanentItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenItemIsNotPermanentEligible()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.TryAddRunItem(CreateItem("item.consumable.baume"));

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.consumable.baume", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(CreateSnapshot("item.consumable.baume", isPermanentEligible: false)));

        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new ConfirmPermanentItemSelectionCommandHandler(
            repository.Object, catalogGateway.Object, playerProfileGateway);

        var act = () => handler.Handle(
            new ConfirmPermanentItemSelectionCommand(run.Id.Value, ["item.consumable.baume"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        playerProfileGateway.AddedPermanentItems.Should().BeEmpty();
    }

    private static RunItem CreateItem(string definitionKey) => RunItem.Create(
        definitionKey, definitionKey, "Un objet de test.",
        RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Heal, 10);

    private static CatalogItemDefinitionSnapshot CreateSnapshot(string key, bool isPermanentEligible) => new(
        key, "1.0", key, "Description.", null,
        "Consumable", "Heal", "Common", "UseInCombat", "RuntimeRunOnly", "Additive",
        99, true, true,
        IsPermanentEligible: isPermanentEligible,
        EquipmentEffects: []);
}
