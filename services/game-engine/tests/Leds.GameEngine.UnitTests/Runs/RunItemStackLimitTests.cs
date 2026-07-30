using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunItemStackLimitTests
{
    [Fact]
    public void Consumable_stack_defaults_to_twenty()
    {
        var item = RunItem.Create(
            "item.the-seuil",
            "Thé du seuil",
            "Restaure la Vitalité.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 19,
            RunItemEffectType.Heal,
            effectAmount: 25);

        item.AddQuantity(1);

        item.Quantity.Should().Be(20);
        item.EffectiveMaxStack.Should().Be(20);
    }

    [Fact]
    public void Consumable_stack_cannot_exceed_twenty()
    {
        var item = RunItem.Create(
            "item.the-seuil",
            "Thé du seuil",
            "Restaure la Vitalité.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 20,
            RunItemEffectType.Heal,
            effectAmount: 25);

        var act = () => item.AddQuantity(1);

        act.Should().Throw<DomainException>()
            .WithMessage("*stack limit of 20*");
    }

    [Fact]
    public void Authored_stack_limit_is_respected()
    {
        var item = RunItem.Rehydrate(
            RunItemId.New(),
            "item.page-arrachee",
            "Page arrachée",
            "Objet unique.",
            RunItemType.Consumable,
            RunItemRarity.Epic,
            quantity: 1,
            RunItemEffectType.None,
            effectAmount: 0,
            createdAtUtc: DateTime.UtcNow,
            maxStack: 1);

        item.CanAddQuantity(1).Should().BeFalse();
    }
}
