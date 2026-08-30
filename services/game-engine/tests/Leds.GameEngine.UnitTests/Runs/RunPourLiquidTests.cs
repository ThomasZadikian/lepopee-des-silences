using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunPourLiquidTests
{
    private static RunItem CreateContainer() =>
        RunItem.Create(
            "item.fiole-cristal", "Fiole de cristal", "",
            RunItemType.Passive, RunItemRarity.Rare,
            quantity: 1,
            effectType: RunItemEffectType.None,
            effectAmount: 0,
            isContainer: true,
            containerCapacity: 1);

    private static RunItem CreateLiquid(int quantity = 1) =>
        RunItem.Create(
            "item.larme-de-racine", "Larme de racine", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: quantity,
            effectType: RunItemEffectType.Heal,
            effectAmount: 12,
            isLiquid: true);

    [Fact]
    public void PourLiquid_ShouldFillContainer_AndConsumeOneUnitOfLiquid()
    {
        var run = TestGameEngineFactory.CreateRun();
        var container = CreateContainer();
        var liquid = CreateLiquid(quantity: 2);
        run.AddRunItem(container);
        run.AddRunItem(liquid);

        var depleted = run.PourLiquid(container.Id, liquid.Id);

        depleted.Should().BeFalse();
        container.ContainedLiquidDefinitionKey.Should().Be("item.larme-de-racine");
        liquid.Quantity.Should().Be(1);
    }

    [Fact]
    public void PourLiquid_ShouldReportDepleted_WhenLastUnitPoured()
    {
        var run = TestGameEngineFactory.CreateRun();
        var container = CreateContainer();
        var liquid = CreateLiquid(quantity: 1);
        run.AddRunItem(container);
        run.AddRunItem(liquid);

        var depleted = run.PourLiquid(container.Id, liquid.Id);

        depleted.Should().BeTrue();
        liquid.Quantity.Should().Be(0);
    }

    [Fact]
    public void PourLiquid_ShouldThrow_WhenSourceItemIsNotALiquid()
    {
        var run = TestGameEngineFactory.CreateRun();
        var container = CreateContainer();
        var notALiquid = RunItem.Create(
            "item.datura", "Datura", "",
            RunItemType.Consumable, RunItemRarity.Rare,
            quantity: 1, effectType: RunItemEffectType.Heal, effectAmount: 18);
        run.AddRunItem(container);
        run.AddRunItem(notALiquid);

        var act = () => run.PourLiquid(container.Id, notALiquid.Id);

        act.Should().Throw<DomainException>().WithMessage("*not a liquid*");
    }

    [Fact]
    public void PourLiquid_ShouldThrow_WhenContainerItemIdNotFound()
    {
        var run = TestGameEngineFactory.CreateRun();
        var liquid = CreateLiquid();
        run.AddRunItem(liquid);

        var act = () => run.PourLiquid(new RunItemId(Guid.NewGuid()), liquid.Id);

        act.Should().Throw<DomainException>().WithMessage("*not found in run inventory*");
    }

    [Fact]
    public void EmptyContainer_ShouldClearContainedLiquid()
    {
        var run = TestGameEngineFactory.CreateRun();
        var container = CreateContainer();
        var liquid = CreateLiquid();
        run.AddRunItem(container);
        run.AddRunItem(liquid);
        run.PourLiquid(container.Id, liquid.Id);

        run.EmptyContainer(container.Id);

        container.ContainedLiquidDefinitionKey.Should().BeNull();
    }
}
