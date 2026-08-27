using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterItemTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldRejectMissingDefinitionKey(string key)
    {
        var act = () => PlayerCharacterItem.Create(key, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("Item definition key is required.");
    }

    [Fact]
    public void Create_ShouldNormalizeDefinitionAndSource()
    {
        var acquiredAt = DateTimeOffset.UtcNow;

        var item = PlayerCharacterItem.Create(
            "  item.sword  ", acquiredAt, "  reward  ", isEquipped: true, slot: EquipmentSlotKind.Weapon);

        item.ItemDefinitionKey.Should().Be("item.sword");
        item.Source.Should().Be("reward");
        item.IsEquipped.Should().BeTrue();
        item.Slot.Should().Be(EquipmentSlotKind.Weapon);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldNormalizeMissingSourceToNull(string? source)
    {
        var item = PlayerCharacterItem.Create("item.relic", DateTimeOffset.UtcNow, source);

        item.Source.Should().BeNull();
        item.IsEquipped.Should().BeFalse();
        item.Slot.Should().Be(EquipmentSlotKind.Relic);
    }
}
