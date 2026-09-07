using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCharacterSnapshotCoverageTests
{
    [Fact]
    public void Create_ShouldNormalizeEquipmentAndInitializeResources()
    {
        var snapshot = RunCharacterSnapshot.Create(
            Guid.NewGuid(),
            " character.test ",
            " Hero ",
            Stats(maxVitality: 100, mana: 40),
            [Skill("skill.one")],
            [" item.one ", "ITEM.ONE", " ", "item.two"],
            "Neutral");

        snapshot.DefinitionKey.Should().Be("character.test");
        snapshot.DisplayName.Should().Be("Hero");
        snapshot.CurrentVitality.Should().Be(100);
        snapshot.CurrentMana.Should().Be(40);
        snapshot.EquippedItemKeys.Should().BeEquivalentTo(["item.one", "item.two"]);
    }

    [Fact]
    public void Create_ShouldAllowNullEquipment()
    {
        var snapshot = RunCharacterSnapshot.Create(
            Guid.NewGuid(), "character.test", "Hero", Stats(), [Skill("skill.one")],
            equippedItemKeys: null, emotionalRegisterCode: "Neutral");

        snapshot.EquippedItemKeys.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRejectEmptyCharacterId()
    {
        var action = () => RunCharacterSnapshot.Create(
            Guid.Empty, "character.test", "Hero", Stats(), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral");

        action.Should().Throw<DomainException>().WithMessage("*Character id is required*");
    }

    [Theory]
    [InlineData("", "Hero", "definition key")]
    [InlineData("character.test", "", "display name")]
    public void Create_ShouldRejectRequiredText(string definitionKey, string displayName, string expected)
    {
        var action = () => RunCharacterSnapshot.Create(
            Guid.NewGuid(), definitionKey, displayName, Stats(), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral");

        action.Should().Throw<DomainException>().WithMessage($"*{expected}*");
    }

    [Fact]
    public void Create_ShouldRejectNullStatBlock()
    {
        var action = () => RunCharacterSnapshot.Create(
            Guid.NewGuid(), "character.test", "Hero", null!, [Skill("skill.one")],
            emotionalRegisterCode: "Neutral");

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReplaceSkills_ShouldRejectNullOrEmptyAndReplaceValidSkills()
    {
        var snapshot = Create();

        var nullAction = () => snapshot.ReplaceSkills(null!);
        nullAction.Should().Throw<DomainException>();
        var emptyAction = () => snapshot.ReplaceSkills([]);
        emptyAction.Should().Throw<DomainException>();

        snapshot.ReplaceSkills([Skill("skill.two")]);
        snapshot.Skills.Should().ContainSingle().Which.SkillDefinitionKey.Should().Be("skill.two");
    }

    [Fact]
    public void ReplaceEquippedItemKeys_ShouldNormalizeNullWhitespaceAndDuplicates()
    {
        var snapshot = Create();

        snapshot.ReplaceEquippedItemKeys([" a ", "A", "", "b"]);
        snapshot.EquippedItemKeys.Should().BeEquivalentTo(["a", "b"]);

        snapshot.ReplaceEquippedItemKeys(null);
        snapshot.EquippedItemKeys.Should().BeEmpty();
    }

    [Fact]
    public void EquipmentLoadout_ShouldRejectDuplicateInstancesAndPositions()
    {
        var instance = Guid.NewGuid();
        var duplicateInstance = () => RunCharacterSnapshot.Create(
            Guid.NewGuid(), "character.test", "Hero", Stats(), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral",
            equipmentLoadout:
            [
                new(Guid.NewGuid(), instance, "item.one", "Ring1"),
                new(Guid.NewGuid(), instance, "item.one", "Ring2")
            ]);
        var duplicatePosition = () => RunCharacterSnapshot.Create(
            Guid.NewGuid(), "character.test", "Hero", Stats(), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral",
            equipmentLoadout:
            [
                new(Guid.NewGuid(), Guid.NewGuid(), "item.one", "Ring1"),
                new(Guid.NewGuid(), Guid.NewGuid(), "item.two", "Ring1")
            ]);

        duplicateInstance.Should().Throw<DomainException>();
        duplicatePosition.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-10, -10, 0, 0)]
    [InlineData(150, 90, 100, 40)]
    [InlineData(60, 20, 60, 20)]
    public void UpdateCurrentResources_ShouldClampToStatCaps(
        int vitality,
        int mana,
        int expectedVitality,
        int expectedMana)
    {
        var snapshot = Create();

        snapshot.UpdateCurrentResources(vitality, mana);

        snapshot.CurrentVitality.Should().Be(expectedVitality);
        snapshot.CurrentMana.Should().Be(expectedMana);
    }

    [Theory]
    [InlineData(-5, -2, 0, 0)]
    [InlineData(500, 500, 100, 40)]
    [InlineData(75, 15, 75, 15)]
    public void Rehydrate_ShouldClampResourceValues(
        int currentVitality,
        int currentMana,
        int expectedVitality,
        int expectedMana)
    {
        var snapshot = RunCharacterSnapshot.Rehydrate(
            Guid.NewGuid(), Guid.NewGuid(), "character.test", "Hero",
            Stats(maxVitality: 100, mana: 40), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral",
            currentVitality: currentVitality,
            currentMana: currentMana);

        snapshot.CurrentVitality.Should().Be(expectedVitality);
        snapshot.CurrentMana.Should().Be(expectedMana);
    }

    private static RunCharacterSnapshot Create() =>
        RunCharacterSnapshot.Create(
            Guid.NewGuid(), "character.test", "Hero", Stats(), [Skill("skill.one")],
            emotionalRegisterCode: "Neutral");

    private static RunCharacterStatSnapshot Stats(int maxVitality = 100, int mana = 40) =>
        RunCharacterStatSnapshot.Create(
            maxVitality, attackPower: 10, defense: 5, startingGuard: 0,
            speed: 10, initiative: 10, focus: 5, mana: mana, charge: 0);

    private static RunCharacterSkillSnapshot Skill(string key) =>
        RunCharacterSkillSnapshot.Create(
            key, key, "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, emotionalRegister: "Neutral");
}
