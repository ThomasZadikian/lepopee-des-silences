using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetRunInventory;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetRunInventory;

public sealed class GetRunInventoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnInventory_WhenRunHasItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var item1 = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 2,
            RunItemEffectType.Heal,
            effectAmount: 15);
        var item2 = RunItem.Create(
            "item.consumable.guard-shard",
            "Éclat de garde",
            "Grants guard",
            RunItemType.Consumable,
            RunItemRarity.Uncommon,
            quantity: 1,
            RunItemEffectType.Guard,
            effectAmount: 8);
        run.AddRunItem(item1);
        run.AddRunItem(item2);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new GetRunInventoryQueryHandler(runRepo.Object);

        var response = await handler.Handle(
            new GetRunInventoryQuery(run.Id.Value),
            CancellationToken.None);

        response.RunId.Should().Be(run.Id.Value);
        response.Items.Should().HaveCount(2);
        response.Items.Should().Contain(i => i.DefinitionKey == "item.consumable.minor-heal");
        response.Items.Should().Contain(i => i.DefinitionKey == "item.consumable.guard-shard");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyInventory_WhenRunHasNoItems()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new GetRunInventoryQueryHandler(runRepo.Object);

        var response = await handler.Handle(
            new GetRunInventoryQuery(run.Id.Value),
            CancellationToken.None);

        response.RunId.Should().Be(run.Id.Value);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new GetRunInventoryQueryHandler(runRepo.Object);

        var act = () => handler.Handle(
            new GetRunInventoryQuery(runId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }
}
