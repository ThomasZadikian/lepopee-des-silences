using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class EquipmentLoadoutTests
{
    [Fact]
    public void Equip_ShouldReplaceOccupiedPositionAtomically()
    {
        var character = CreateCharacter();
        var first = PlayerCharacterItem.Create("item.ring", DateTimeOffset.UtcNow);
        var second = PlayerCharacterItem.Create("item.ring", DateTimeOffset.UtcNow);
        character.AddItem(first);
        character.AddItem(second);
        character.EquipItem(first.Id, EquipmentPosition.Ring1);

        character.EquipItem(second.Id, EquipmentPosition.Ring1);

        character.EquipmentLoadout[EquipmentPosition.Ring1].Should().Be(second.Id);
        first.IsEquipped.Should().BeFalse();
        second.IsEquipped.Should().BeTrue();
    }

    [Fact]
    public void AddItem_ShouldAllowTwoInstancesOfSameDefinition()
    {
        var character = CreateCharacter();
        var first = PlayerCharacterItem.Create("item.ring", DateTimeOffset.UtcNow);
        var second = PlayerCharacterItem.Create("item.ring", DateTimeOffset.UtcNow);

        character.AddItem(first);
        character.AddItem(second);
        character.EquipItem(first.Id, EquipmentPosition.Ring1);
        character.EquipItem(second.Id, EquipmentPosition.Ring2);

        character.Items.Should().HaveCount(2);
        character.EquipmentLoadout.Values.Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public void Equip_ShouldRejectSameInstanceInTwoPositionsWithoutMutation()
    {
        var character = CreateCharacter();
        var ring = PlayerCharacterItem.Create("item.ring", DateTimeOffset.UtcNow);
        character.AddItem(ring);
        character.EquipItem(ring.Id, EquipmentPosition.Ring1);

        var act = () => character.EquipItem(ring.Id, EquipmentPosition.Ring2);

        act.Should().Throw<DomainException>().WithMessage("*already equipped*");
        character.EquipmentLoadout.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<EquipmentPosition, OwnedItemInstanceId>(EquipmentPosition.Ring1, ring.Id));
    }

    [Fact]
    public void EquipmentVocabulary_ShouldReserveOffWeapon()
    {
        Enum.GetValues<EquipmentSlotKind>().Should().Contain(EquipmentSlotKind.OffWeapon);
        Enum.GetValues<EquipmentPosition>().Should().Contain(EquipmentPosition.OffWeapon);
        EquipmentPositionCompatibility.Accepts(EquipmentPosition.Ring1, EquipmentSlotKind.Ring).Should().BeTrue();
        EquipmentPositionCompatibility.Accepts(EquipmentPosition.Ring2, EquipmentSlotKind.Ring).Should().BeTrue();
        EquipmentPositionCompatibility.Accepts(EquipmentPosition.Hand, EquipmentSlotKind.MainWeapon).Should().BeFalse();
    }

    public static TheoryData<EquipmentPosition, EquipmentSlotKind> CompatiblePositions => new()
    {
        { EquipmentPosition.Head, EquipmentSlotKind.Head },
        { EquipmentPosition.Neck, EquipmentSlotKind.Neck },
        { EquipmentPosition.Shoulders, EquipmentSlotKind.Shoulders },
        { EquipmentPosition.Cape, EquipmentSlotKind.Cape },
        { EquipmentPosition.Chest, EquipmentSlotKind.Chest },
        { EquipmentPosition.Wrist, EquipmentSlotKind.Wrist },
        { EquipmentPosition.Hand, EquipmentSlotKind.Hand },
        { EquipmentPosition.Waist, EquipmentSlotKind.Waist },
        { EquipmentPosition.Legs, EquipmentSlotKind.Legs },
        { EquipmentPosition.Feet, EquipmentSlotKind.Feet },
        { EquipmentPosition.Ring1, EquipmentSlotKind.Ring },
        { EquipmentPosition.Ring2, EquipmentSlotKind.Ring },
        { EquipmentPosition.Relic, EquipmentSlotKind.Relic },
        { EquipmentPosition.MainWeapon, EquipmentSlotKind.MainWeapon },
        { EquipmentPosition.MainWeapon, EquipmentSlotKind.Weapon },
        { EquipmentPosition.OffWeapon, EquipmentSlotKind.OffWeapon }
    };

    [Theory]
    [MemberData(nameof(CompatiblePositions))]
    public void Compatibility_ShouldAcceptEveryDeclaredPosition(
        EquipmentPosition position,
        EquipmentSlotKind slot)
    {
        EquipmentPositionCompatibility.Accepts(position, slot).Should().BeTrue();
    }

    [Fact]
    public void Compatibility_ShouldRejectUnknownPosition()
    {
        EquipmentPositionCompatibility.Accepts((EquipmentPosition)999, EquipmentSlotKind.Relic)
            .Should().BeFalse();
    }

    private static PlayerCharacter CreateCharacter() => PlayerCharacter.Create(
        "character.test",
        "Test",
        PlayerCharacterStatBlock.CreateDefaultPorteur(),
        [PlayerCharacterSkill.Create("skill.test", DateTimeOffset.UtcNow)]);
}
