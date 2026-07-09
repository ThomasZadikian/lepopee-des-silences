using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.Application.Enemies.Definitions;

public sealed class EnemyDefinitionTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var def = EnemyDefinition.Create(
            "enemy.threshold.doubt-fragment",
            "Fragment de Doute",
            "Une hesitation cristallisee.",
            "1.0.0",
            "Fragile",
            baseDifficulty: 1,
            minRiskLevel: 1,
            maxRiskLevel: 2,
            compatibleRoomTypes: ["Threshold"],
            tags: ["threshold", "doubt"],
            skillKeys: ["skill.basic.strike"]);

        def.Id.Value.Should().NotBeEmpty();
        def.Key.Value.Should().Be("enemy.threshold.doubt-fragment");
        def.Name.Value.Should().Be("Fragment de Doute");
        def.Description.Value.Should().Be("Une hesitation cristallisee.");
        def.Version.Value.Should().Be("1.0.0");
        def.Status.Should().Be(CatalogContentStatus.Draft);
        def.Archetype.Should().Be("Fragile");
        def.BaseDifficulty.Should().Be(1);
        def.MinRiskLevel.Should().Be(1);
        def.MaxRiskLevel.Should().Be(2);
        def.CompatibleRoomTypes.Should().BeEquivalentTo("Threshold");
        def.Tags.Should().BeEquivalentTo("threshold", "doubt");
        def.SkillKeys.Should().BeEquivalentTo("skill.basic.strike");
        def.AttackPower.Should().Be(0);
        def.Defense.Should().Be(0);
        def.Speed.Should().Be(10);
    }

    [Fact]
    public void Create_ShouldSetAttackDefenseSpeed_WhenProvided()
    {
        var def = EnemyDefinition.Create(
            "enemy.threshold.doubt-fragment",
            "Fragment de Doute",
            "Une hesitation cristallisee.",
            "1.0.0",
            "Fragile",
            baseDifficulty: 1,
            minRiskLevel: 1,
            maxRiskLevel: 2,
            compatibleRoomTypes: ["Threshold"],
            tags: ["threshold", "doubt"],
            skillKeys: ["skill.basic.strike"],
            attackPower: 15,
            defense: 8,
            speed: 12);

        def.AttackPower.Should().Be(15);
        def.Defense.Should().Be(8);
        def.Speed.Should().Be(12);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAttackPowerIsNegative()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2, ["Threshold"], [], [],
            attackPower: -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition attack power cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDefenseIsNegative()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2, ["Threshold"], [], [],
            defense: -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition defense cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSpeedIsLessThanOne()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2, ["Threshold"], [], [],
            speed: 0);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition speed must be at least 1.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenKeyIsEmpty()
    {
        var act = () => EnemyDefinition.Create(
            "", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2, ["Threshold"], [], []);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenDescriptionIsEmpty()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", null, "1.0.0", "Fragile", 1, 1, 2, ["Threshold"], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition description is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenArchetypeIsEmpty()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "", 1, 1, 2, ["Threshold"], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition archetype is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCompatibleRoomTypesIsEmpty()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2, [], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition must have at least one compatible room type.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenBaseDifficultyIsZero()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 0, 1, 2, ["Threshold"], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition base difficulty must be greater than 0.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMinRiskLevelIsZero()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 0, 2, ["Threshold"], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition min risk level must be at least 1.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxRiskLevelIsLessThanMin()
    {
        var act = () => EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 3, 2, ["Threshold"], [], []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Enemy definition max risk level must be greater than or equal to min risk level.");
    }

    [Fact]
    public void Create_ShouldDeduplicateTags()
    {
        var def = EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2,
            ["Threshold"], ["tag1", "tag1", "tag2"], []);

        def.Tags.Should().BeEquivalentTo("tag1", "tag2");
    }

    [Fact]
    public void Create_ShouldDeduplicateSkillKeys()
    {
        var def = EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2,
            ["Threshold"], [], ["sk1", "sk1", "sk2"]);

        def.SkillKeys.Should().BeEquivalentTo("sk1", "sk2");
    }

    [Fact]
    public void Create_ShouldDeduplicateCompatibleRoomTypes()
    {
        var def = EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2,
            ["Threshold", "Threshold", "Forest"], [], []);

        def.CompatibleRoomTypes.Should().BeEquivalentTo("Threshold", "Forest");
    }

    [Fact]
    public void Create_ShouldHandleNullTags()
    {
        var def = EnemyDefinition.Create(
            "enemy.test", "Name", "Desc", "1.0.0", "Fragile", 1, 1, 2,
            ["Threshold"], null, null);

        def.Tags.Should().BeEmpty();
        def.SkillKeys.Should().BeEmpty();
    }
}
