using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunItemConsumeOneTests
{
    [Fact]
    public void ConsumeOne_ShouldDecrementQuantity_WhenItemIsConsumable()
    {
        var item = RunItem.Create(
            "item.potion.v1", "Potion", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: 3,
            effectType: RunItemEffectType.Heal,
            effectAmount: 10);

        item.ConsumeOne();

        item.Quantity.Should().Be(2);
    }

    [Fact]
    public void ConsumeOne_ShouldThrow_WhenItemIsNotConsumable()
    {
        var item = RunItem.Create(
            "item.shard.v1", "Éclat", "",
            RunItemType.Passive, RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.Guard,
            effectAmount: 8);

        var act = () => item.ConsumeOne();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not consumable*");
    }

    [Fact]
    public void ConsumeOne_ShouldThrow_WhenQuantityIsZero()
    {
        var item = RunItem.Create(
            "item.potion.v1", "Potion", "",
            RunItemType.Consumable, RunItemRarity.Common,
            quantity: 1,
            effectType: RunItemEffectType.Heal,
            effectAmount: 10);

        item.ConsumeOne(); // passe à 0

        var act = () => item.ConsumeOne();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*no remaining uses*");
    }
}