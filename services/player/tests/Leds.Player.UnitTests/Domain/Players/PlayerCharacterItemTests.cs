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

    public static TheoryData<EquipmentSlotKind, EquipmentPosition> LegacyPositions => new()
    {
        { EquipmentSlotKind.MainWeapon, EquipmentPosition.MainWeapon },
        { EquipmentSlotKind.Weapon, EquipmentPosition.MainWeapon },
        { EquipmentSlotKind.Ring, EquipmentPosition.Ring1 },
        { EquipmentSlotKind.Accessory, EquipmentPosition.Neck },
        { EquipmentSlotKind.Head, EquipmentPosition.Head },
        { EquipmentSlotKind.Neck, EquipmentPosition.Neck },
        { EquipmentSlotKind.Shoulders, EquipmentPosition.Shoulders },
        { EquipmentSlotKind.Cape, EquipmentPosition.Cape },
        { EquipmentSlotKind.Chest, EquipmentPosition.Chest },
        { EquipmentSlotKind.Wrist, EquipmentPosition.Wrist },
        { EquipmentSlotKind.Hand, EquipmentPosition.Hand },
        { EquipmentSlotKind.Waist, EquipmentPosition.Waist },
        { EquipmentSlotKind.Legs, EquipmentPosition.Legs },
        { EquipmentSlotKind.Feet, EquipmentPosition.Feet },
        { EquipmentSlotKind.OffWeapon, EquipmentPosition.OffWeapon },
        { EquipmentSlotKind.Relic, EquipmentPosition.Relic }
    };

    [Theory]
    [MemberData(nameof(LegacyPositions))]
    public void Create_ShouldMapLegacySlotsToConcretePositions(
        EquipmentSlotKind slot,
        EquipmentPosition expected)
    {
        var item = PlayerCharacterItem.Create(
            "item.test", DateTimeOffset.UtcNow, isEquipped: true, slot: slot);

        item.Position.Should().Be(expected);
    }

    [Fact]
    public void Rehydrate_ShouldValidateIdentityAndNormalizeValues()
    {
        var id = OwnedItemInstanceId.New();
        var item = PlayerCharacterItem.Rehydrate(
            id, "  item.test  ", DateTimeOffset.UtcNow, "  persisted  ", EquipmentPosition.Chest);

        item.Id.Should().Be(id);
        item.ItemDefinitionKey.Should().Be("item.test");
        item.Source.Should().Be("persisted");
        item.Position.Should().Be(EquipmentPosition.Chest);
    }

    [Fact]
    public void Rehydrate_ShouldRejectEmptyIdentityAndDefinition()
    {
        var emptyId = () => PlayerCharacterItem.Rehydrate(
            new OwnedItemInstanceId(Guid.Empty), "item.test", DateTimeOffset.UtcNow, null, null);
        var emptyDefinition = () => PlayerCharacterItem.Rehydrate(
            OwnedItemInstanceId.New(), " ", DateTimeOffset.UtcNow, null, null);

        emptyId.Should().Throw<DomainException>();
        emptyDefinition.Should().Throw<DomainException>();
    }
}
