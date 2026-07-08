using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Skills;
using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class SkillDefinitionTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var def = SkillDefinition.Create(
            "skill.basic.strike",
            "Frappe",
            "Une attaque de base.",
            "1.0.0",
            "Damage",
            "SingleEnemy",
            "Damage",
            manaCost: 5,
            chargeCost: 0,
            basePower: 10);

        def.Id.Value.Should().NotBeEmpty();
        def.Key.Value.Should().Be("skill.basic.strike");
        def.Name.Value.Should().Be("Frappe");
        def.Description.Value.Should().Be("Une attaque de base.");
        def.Version.Value.Should().Be("1.0.0");
        def.Status.Should().Be(CatalogContentStatus.Draft);
        def.SkillType.Should().Be("Damage");
        def.TargetingType.Should().Be("SingleEnemy");
        def.EffectType.Should().Be("Damage");
        def.ManaCost.Should().Be(5);
        def.ChargeCost.Should().Be(0);
        def.BasePower.Should().Be(10);
    }

    [Fact]
    public void Create_ShouldThrow_WhenKeyIsEmpty()
    {
        var act = () => SkillDefinition.Create(
            "", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenDescriptionIsEmpty()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", null, "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition description is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSkillTypeIsEmpty()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "", "SingleEnemy", "Damage", 5, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition skill type is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenTargetingTypeIsEmpty()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "", "Damage", 5, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition targeting type is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenEffectTypeIsEmpty()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "", 5, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition effect type is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenManaCostIsNegative()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", -1, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition mana cost cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenChargeCostIsNegative()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, -1, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition charge cost cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenBasePowerIsNegative()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition base power cannot be negative.");
    }

    [Fact]
    public void Create_ShouldDefaultToEmptyEffects_WhenNoneProvided()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10);

        def.Effects.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldCarryMultipleSimultaneousEffects()
    {
        var effects = new[]
        {
            new SkillEffectSpec("HealOverTime", null, 10, 5000, TickInterval: 1000, MagnitudeIsPercentOfMax: true),
            new SkillEffectSpec("GuardOverTime", null, 8, 5000, TickInterval: 1000)
        };

        var def = SkillDefinition.Create(
            "skill.construction-perpetuelle", "Construction perpétuelle", "Desc", "1.0.0",
            "Buff", "Self", "Buff", 14, 0, 0, effects: effects);

        def.Effects.Should().HaveCount(2);
        def.Effects.Should().Contain(e => e.Kind == "HealOverTime" && e.MagnitudeIsPercentOfMax);
        def.Effects.Should().Contain(e => e.Kind == "GuardOverTime" && !e.MagnitudeIsPercentOfMax);
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Buff", "Self", "Buff", 2, 0, 0);

        def.Activate();

        def.Status.Should().Be(CatalogContentStatus.Active);
        def.IsActive.Should().BeTrue();
    }
}
