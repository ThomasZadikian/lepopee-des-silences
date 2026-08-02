using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs.Dtos;

public sealed class RunItemDtoTests
{
    [Fact]
    public void FromDomain_ShouldExposeIsUsable_ForABattleUsableConsumable()
    {
        var item = RunItem.Create(
            "item.healing-draught",
            "Potion de soin",
            "Restaure de la vitalité.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.Heal,
            effectAmount: 20);

        var dto = RunItemDto.FromDomain(item);

        item.IsUsable.Should().BeTrue();
        dto.IsUsable.Should().BeTrue();
    }

    [Fact]
    public void FromDomain_ShouldExposeIsUsableAsFalse_ForANonBattleEffect()
    {
        var item = RunItem.Create(
            "item.silent-trinket",
            "Babiole silencieuse",
            "Ne fait rien en combat.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity: 1,
            RunItemEffectType.None,
            effectAmount: 0);

        var dto = RunItemDto.FromDomain(item);

        item.IsUsable.Should().BeFalse();
        dto.IsUsable.Should().BeFalse();
    }
}
