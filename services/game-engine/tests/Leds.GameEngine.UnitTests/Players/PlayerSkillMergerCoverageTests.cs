using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Players;

public sealed class PlayerSkillMergerCoverageTests
{
    private readonly Mock<ICatalogContentGateway> _gateway = new();

    [Fact]
    public async Task CollectEquippedItemEffects_ShouldCoverEmptyMissingAndPresentEffects()
    {
        var sut = new PlayerSkillMerger(_gateway.Object);
        (await sut.CollectEquippedItemEffectsAsync([], default)).Should().BeEmpty();

        _gateway.Setup(g => g.GetItemDefinitionByKeyAsync("missing", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("missing", "missing")));
        await FluentActions.Awaiting(() => sut.CollectEquippedItemEffectsAsync(["missing"], default))
            .Should().ThrowAsync<InvalidOperationException>();

        _gateway.Setup(g => g.GetItemDefinitionByKeyAsync("empty", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(Item("empty", null)));
        _gateway.Setup(g => g.GetItemDefinitionByKeyAsync("grant", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(Item("grant",
                [new CatalogItemEquipmentEffect("GrantSkill", null, null, "skill.granted", null)])));

        var effects = await sut.CollectEquippedItemEffectsAsync(["empty", "grant"], default);
        effects.Should().ContainSingle().Which.SkillKey.Should().Be("skill.granted");
    }

    [Fact]
    public async Task ResolveEquippedItems_ShouldValidateAndRejectMissingDefinitions()
    {
        var sut = new PlayerSkillMerger(_gateway.Object);
        _gateway.Setup(g => g.GetItemDefinitionByKeyAsync("valid", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(Item("valid", null)));
        var resolved = await sut.ResolveEquippedItemsAsync(["valid"], default);
        resolved.Should().ContainSingle();

        _gateway.Setup(g => g.GetItemDefinitionByKeyAsync("missing", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("missing", "missing")));
        await FluentActions.Awaiting(() => sut.ResolveEquippedItemsAsync(["missing"], default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MergeSkills_ShouldUseCatalogIgnoreDuplicateGrantsAndAddNewGrant()
    {
        var learned = Skill("skill.learned", 12);
        var granted = Skill("skill.granted", 25);
        SetupSkill(learned);
        SetupSkill(granted);

        var character = Character([Fallback("skill.learned")]);
        var effects = new CatalogItemEquipmentEffect[]
        {
            new("GrantSkill", null, null, "skill.learned", null),
            new("GrantSkill", null, null, "skill.granted", null),
            new("GrantSkill", null, null, "SKILL.GRANTED", null),
            new("GrantSkill", null, null, " ", null),
            new("HitChanceBonus", null, 5, null, null)
        };

        var merged = await new PlayerSkillMerger(_gateway.Object)
            .MergeSkillsAsync(character, effects, default);

        merged.Should().HaveCount(2);
        merged.Single(s => s.Key == "skill.learned").BasePower.Should().Be(12);
        merged.Single(s => s.Key == "skill.granted").BasePower.Should().Be(25);
    }

    [Fact]
    public async Task MergeSkills_ShouldRejectMissingLearnedOrGrantedSkill()
    {
        var sut = new PlayerSkillMerger(_gateway.Object);
        _gateway.Setup(g => g.GetSkillDefinitionByKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((CatalogSkillDefinition?)null);

        await FluentActions.Awaiting(() => sut.MergeSkillsAsync(Character([Fallback("missing")]), [], default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*could not be resolved*");

        await FluentActions.Awaiting(() => sut.MergeSkillsAsync(Character([]),
                [new CatalogItemEquipmentEffect("GrantSkill", null, null, "missing.grant", null)], default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*could not be resolved*");
    }

    [Fact]
    public async Task MergeSkills_ShouldApplyWeaponContractAndFallbacksToBasicStrike()
    {
        SetupSkill(Skill("skill.basic.strike", 99));
        var weapon = Item("weapon", null, category: "Weapon", displayName: "Épée",
            basicAttackPower: null, basicAttackCategory: " ", tacticalRange: 0,
            tacticalAreaShape: " ", requiresLineOfSight: true);

        var result = await new PlayerSkillMerger(_gateway.Object).MergeSkillsAsync(
            Character([Fallback("skill.basic.strike")]), [], default, weapon);

        var strike = result.Single();
        strike.DisplayName.Should().Be("Attaque — Épée");
        strike.BasePower.Should().Be(10);
        strike.Category.Should().Be("Physical");
        strike.TacticalRange.Should().Be(1);
        strike.TacticalAreaShape.Should().Be("Single");
        strike.RequiresLineOfSight.Should().BeTrue();
    }

    [Theory]
    [InlineData("skill.other", "Weapon")]
    [InlineData("skill.basic.strike", "Equipment")]
    public async Task MergeSkills_ShouldNotApplyWeaponContractWhenNotBasicWeapon(string skillKey, string category)
    {
        SetupSkill(Skill(skillKey, 17));
        var result = await new PlayerSkillMerger(_gateway.Object).MergeSkillsAsync(
            Character([Fallback(skillKey)]), [], default,
            Item("item", null, category: category, displayName: "Objet", basicAttackPower: 80));
        result.Single().BasePower.Should().Be(17);
    }

    private void SetupSkill(CatalogSkillDefinition skill) =>
        _gateway.Setup(g => g.GetSkillDefinitionByKeyAsync(skill.Key, default)).ReturnsAsync(skill);

    private static PlayerRunSnapshotCharacter Character(IReadOnlyCollection<PlayerRunSnapshotCharacterSkill> skills) =>
        new(Guid.NewGuid(), "char.hero", "Hero",
            new PlayerRunSnapshotCharacterStats(100, 10, 5, 0, 10, 0, 0, 20, 0), skills);

    private static PlayerRunSnapshotCharacterSkill Fallback(string key) =>
        new(key, "fallback", "Active", "SingleEnemy", "Damage", 0, 0, 1);

    private static CatalogSkillDefinition Skill(string key, int power) =>
        new(key, key, "desc", "Active", "SingleEnemy", "Damage", 0, 0, power, [], "Mémoire");

    private static CatalogItemDefinitionSnapshot Item(
        string key,
        IReadOnlyCollection<CatalogItemEquipmentEffect>? effects,
        string category = "Equipment",
        string? displayName = null,
        int? basicAttackPower = null,
        string? basicAttackCategory = null,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false) =>
        new(key, "1", displayName ?? key, "desc", null, category, "Accessory", "Common",
            "Equip", "Persistent", "None", 1, false, false,
            EquipmentEffects: effects,
            TacticalRange: tacticalRange,
            TacticalAreaShape: tacticalAreaShape,
            RequiresLineOfSight: requiresLineOfSight,
            BasicAttackPower: basicAttackPower,
            BasicAttackCategory: basicAttackCategory);
}
