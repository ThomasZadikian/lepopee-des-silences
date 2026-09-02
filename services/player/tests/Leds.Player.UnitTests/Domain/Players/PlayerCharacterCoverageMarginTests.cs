using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterCoverageMarginTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldNormalizeBlankCharacterTypeAndStatus()
    {
        var character = PlayerCharacter.Create(
            "character.test",
            "Test",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [Skill("skill.one")],
            characterType: " ",
            status: " ");

        character.CharacterType.Should().Be("Standard");
        character.Status.Should().Be("Active");
    }

    [Fact]
    public void CreatePlayable_ShouldRejectBlankArchetype()
    {
        var act = () => PlayerCharacter.CreatePlayable(
            "character.test",
            "Test",
            " ",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [Skill("skill.one")]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var character = Character();
        character.Archive(Now);

        character.Archive(Now.AddHours(1));

        character.ArchivedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void AddSkill_ShouldIgnoreDuplicateCaseInsensitively()
    {
        var character = Character();
        character.AddSkill(Skill("SKILL.ONE"));

        character.Skills.Should().HaveCount(1);
    }

    [Fact]
    public void EquipSkill_ShouldBeIdempotentWhenAlreadyEquipped()
    {
        var character = PlayerCharacter.Create(
            "character.test",
            "Test",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [Skill("skill.one", equipped: true)]);

        character.EquipSkill("skill.one");

        character.EquippedCount.Should().Be(1);
    }

    [Fact]
    public void EquipSkill_ShouldRejectFifthManagedSkill()
    {
        var character = PlayerCharacter.Create(
            "character.test",
            "Test",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [
                Skill("skill.one", true),
                Skill("skill.two", true),
                Skill("skill.three", true),
                Skill("skill.four", true),
                Skill("skill.five")
            ]);

        var act = () => character.EquipSkill("skill.five");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SkillOperations_ShouldRejectUnknownSkill()
    {
        var character = Character();

        var equip = () => character.EquipSkill("skill.missing");
        var unequip = () => character.UnequipSkill("skill.missing");

        equip.Should().Throw<DomainException>();
        unequip.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_ShouldIgnoreDuplicateCaseInsensitively()
    {
        var character = Character();
        character.AddItem(Item("item.one"));
        character.AddItem(Item("ITEM.ONE"));

        character.Items.Should().HaveCount(1);
    }

    [Fact]
    public void EquipItem_ShouldBeIdempotentWhenAlreadyEquipped()
    {
        var character = Character();
        character.AddItem(Item("item.one", equipped: true, EquipmentSlotKind.Relic));

        character.EquipItem("item.one", EquipmentSlotKind.Relic);

        character.EquippedItemCount.Should().Be(1);
    }

    [Theory]
    [InlineData(EquipmentSlotKind.Weapon, PlayerCharacter.MaxEquippedWeapons)]
    [InlineData(EquipmentSlotKind.Accessory, PlayerCharacter.MaxEquippedAccessories)]
    [InlineData(EquipmentSlotKind.Relic, PlayerCharacter.MaxEquippedRelics)]
    public void EquipItem_ShouldEnforceEverySlotCapacity(EquipmentSlotKind slot, int limit)
    {
        var character = Character();
        for (var index = 0; index < limit; index++)
        {
            var key = $"item.{index}";
            character.AddItem(Item(key));
            character.EquipItem(key, slot);
        }

        character.AddItem(Item("item.extra"));
        var act = () => character.EquipItem("item.extra", slot);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EquipItem_ShouldRejectUnsupportedSlot()
    {
        var character = Character();
        character.AddItem(Item("item.one"));

        var act = () => character.EquipItem("item.one", (EquipmentSlotKind)999);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ItemOperations_ShouldRejectUnknownItem()
    {
        var character = Character();

        var equip = () => character.EquipItem("missing");
        var unequip = () => character.UnequipItem("missing");

        equip.Should().Throw<DomainException>();
        unequip.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rehydrate_ShouldNormalizeBlankCharacterTypeAndStatus()
    {
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(),
            "character.test",
            "Test",
            " ",
            " ",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [Skill("skill.one")]);

        character.CharacterType.Should().Be("Standard");
        character.Status.Should().Be("Active");
    }

    private static PlayerCharacter Character() => PlayerCharacter.Create(
        "character.test",
        "Test",
        PlayerCharacterStatBlock.CreateDefaultPorteur(),
        [Skill("skill.one")]);

    private static PlayerCharacterSkill Skill(string key, bool equipped = false) =>
        PlayerCharacterSkill.Create(key, Now, "coverage", equipped);

    private static PlayerCharacterItem Item(
        string key,
        bool equipped = false,
        EquipmentSlotKind slot = EquipmentSlotKind.Relic) =>
        PlayerCharacterItem.Create(key, Now, "coverage", equipped, slot);
}
