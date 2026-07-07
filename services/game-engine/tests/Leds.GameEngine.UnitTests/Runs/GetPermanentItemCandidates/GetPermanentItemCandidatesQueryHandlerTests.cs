using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetPermanentItemCandidates;

public sealed class GetPermanentItemCandidatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOnlyPermanentEligibleItems_ObtainedDuringTheRun()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.TryAddRunItem(CreateItem("item.relic.tome"));
        run.TryAddRunItem(CreateItem("item.consumable.baume"));

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.relic.tome", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(CreateSnapshot("item.relic.tome", isPermanentEligible: true)));
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.consumable.baume", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(CreateSnapshot("item.consumable.baume", isPermanentEligible: false)));

        var handler = new GetPermanentItemCandidatesQueryHandler(repository.Object, catalogGateway.Object);

        var response = await handler.Handle(new GetPermanentItemCandidatesQuery(run.Id.Value), CancellationToken.None);

        response.Candidates.Should().ContainSingle();
        response.Candidates.Single().ItemDefinitionKey.Should().Be("item.relic.tome");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>())).ReturnsAsync((Run?)null);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var handler = new GetPermanentItemCandidatesQueryHandler(repository.Object, catalogGateway.Object);

        var act = () => handler.Handle(new GetPermanentItemCandidatesQuery(runId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static RunItem CreateItem(string definitionKey) => RunItem.Create(
        definitionKey, definitionKey, "Un objet de test.",
        RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Heal, 10);

    private static CatalogItemDefinitionSnapshot CreateSnapshot(string key, bool isPermanentEligible) => new(
        key, "1.0", key, "Description.", null,
        "Consumable", "Heal", "Common", "UseInCombat", "RuntimeRunOnly", "Additive",
        99, true, true, null,
        IsPermanentEligible: isPermanentEligible,
        EquipmentEffects: []);
}
