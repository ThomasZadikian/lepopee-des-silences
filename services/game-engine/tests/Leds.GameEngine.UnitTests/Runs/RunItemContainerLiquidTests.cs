using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunItemContainerLiquidTests
{
    private static RunItem CreateContainer(string definitionKey = "item.fiole-cristal") =>
        RunItem.Create(
            definitionKey, "Fiole de cristal", "",
            RunItemType.Passive, RunItemRarity.Rare,
            quantity: 1,
            effectType: RunItemEffectType.None,
            effectAmount: 0,
            isContainer: true,
            containerCapacity: 1);

    private static RunItem CreateLiquid(string definitionKey = "item.larme-de-racine") =>
        RunItem.Create(
            definitionKey, "Larme de racine", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.Heal,
            effectAmount: 12,
            isLiquid: true);

    [Fact]
    public void PourLiquidInto_ShouldSetContainedLiquid_WhenContainerIsEmpty()
    {
        var container = CreateContainer();

        container.PourLiquidInto("item.larme-de-racine");

        container.ContainedLiquidDefinitionKey.Should().Be("item.larme-de-racine");
    }

    [Fact]
    public void PourLiquidInto_ShouldThrow_WhenItemIsNotAContainer()
    {
        var notAContainer = RunItem.Create(
            "item.epee", "Épée", "",
            RunItemType.Passive, RunItemRarity.Common,
            quantity: 1, effectType: RunItemEffectType.None, effectAmount: 0);

        var act = () => notAContainer.PourLiquidInto("item.larme-de-racine");

        act.Should().Throw<DomainException>().WithMessage("*not a container*");
    }

    [Fact]
    public void PourLiquidInto_ShouldThrow_WhenContainerAlreadyHoldsALiquid()
    {
        var container = CreateContainer();
        container.PourLiquidInto("item.larme-de-racine");

        var act = () => container.PourLiquidInto("item.datura");

        act.Should().Throw<DomainException>().WithMessage("*already holds a liquid*");
    }

    [Fact]
    public void EmptyContents_ShouldClearContainedLiquid()
    {
        var container = CreateContainer();
        container.PourLiquidInto("item.larme-de-racine");

        container.EmptyContents();

        container.ContainedLiquidDefinitionKey.Should().BeNull();
    }

    [Fact]
    public void EmptyContents_ShouldThrow_WhenAlreadyEmpty()
    {
        var container = CreateContainer();

        var act = () => container.EmptyContents();

        act.Should().Throw<DomainException>().WithMessage("*already empty*");
    }
}
