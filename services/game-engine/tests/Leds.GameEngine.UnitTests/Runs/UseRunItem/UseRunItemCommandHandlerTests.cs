using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.UseRunItem;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.UseRunItem;

public sealed class UseRunItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUseItem_AndReturnResponse_WhenItemExists()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var result = await handler.Handle(
            new UseRunItemCommand(run.Id.Value, item.Id.Value),
            CancellationToken.None);

        result.RunId.Should().Be(run.Id.Value);
        result.ItemId.Should().Be(item.Id.Value);
        result.EffectType.Should().Be("Heal");
        result.EffectAmount.Should().Be(15);
        result.ItemDepleted.Should().BeTrue();
        result.UsedInCombat.Should().BeFalse();
        result.PlayerState.Should().NotBeNull();

        runRepo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var act = () => handler.Handle(
            new UseRunItemCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenItemNotFound()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var act = () => handler.Handle(
            new UseRunItemCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_ShouldDecrementQuantity_WhenItemHasMultipleQuantity()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 3,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var result = await handler.Handle(
            new UseRunItemCommand(run.Id.Value, item.Id.Value),
            CancellationToken.None);

        result.ItemDepleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRejectLegacyInventoryUse_WhenCombatIsActive()
    {
        var strikeSkill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10, emotionalRegister: "Neutral");
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var run = runWithNode.Run;
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, [strikeSkill]);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, []);
        var combat = TestTacticalCombatHelper.Create(
            run.Id, Domain.Rooms.RoomId.New(), NodeId.New(), [ally], [enemy]);
        run.StartTacticalCombat(combat);

        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var act = () => handler.Handle(
            new UseRunItemCommand(run.Id.Value, item.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*tactical*");
        runRepo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlayerStateDto()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var result = await handler.Handle(
            new UseRunItemCommand(run.Id.Value, item.Id.Value),
            CancellationToken.None);

        result.PlayerState.Should().NotBeNull();
        result.PlayerState!.CurrentVitality.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnNullCombat_WhenNotInCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;
        var item = RunItem.Create(
            "item.consumable.minor-heal",
            "Baume de mémoire",
            "Restores vitality",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 15);
        run.AddRunItem(item);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseRunItemCommandHandler(runRepo.Object);

        var result = await handler.Handle(
            new UseRunItemCommand(run.Id.Value, item.Id.Value),
            CancellationToken.None);

    }

}
