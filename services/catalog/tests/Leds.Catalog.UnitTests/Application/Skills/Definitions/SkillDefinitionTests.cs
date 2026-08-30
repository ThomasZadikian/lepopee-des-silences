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
            basePower: 10,
            emotionalRegister: "Neutral");

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
            "skill.test", "Name", null, "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            emotionalRegister: "Neutral");

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
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            emotionalRegister: "Neutral");

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
            "Buff", "Self", "Buff", 14, 0, 0, effects: effects, emotionalRegister: "Neutral");

        def.Effects.Should().HaveCount(2);
        def.Effects.Should().Contain(e => e.Kind == "HealOverTime" && e.MagnitudeIsPercentOfMax);
        def.Effects.Should().Contain(e => e.Kind == "GuardOverTime" && !e.MagnitudeIsPercentOfMax);
    }

    [Fact]
    public void Create_ShouldRejectUnsupportedEffectKind()
    {
        var act = () => SkillDefinition.Create(
            "skill.invalid-effect", "Invalid", "Desc", "1.0.0",
            "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            effects: [new SkillEffectSpec("UnknownRuntimeEffect", null, 1, 1000)],
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("*is not supported*");
    }

    [Fact]
    public void Create_ShouldRejectStatModifierWithoutSupportedStat()
    {
        var act = () => SkillDefinition.Create(
            "skill.invalid-stat", "Invalid", "Desc", "1.0.0",
            "Buff", "Self", "Buff", 0, 0, 0,
            effects: [new SkillEffectSpec("StatModifier", null, 1, 1000, Stat: "Luck")],
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("*supported Stat*");
    }

    [Fact]
    public void Create_ShouldDefaultCategoryToPhysical_WhenNoneProvided()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            emotionalRegister: "Neutral");

        def.Category.Should().Be("Physical");
    }

    [Fact]
    public void Create_ShouldCarryExplicitCategory()
    {
        var def = SkillDefinition.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage",
            5, 0, 10, category: "Magic", emotionalRegister: "Neutral");

        def.Category.Should().Be("Magic");
    }

    [Fact]
    public void Create_ShouldDefaultCategoryToPhysical_WhenCategoryIsWhitespace()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            category: "   ", emotionalRegister: "Neutral");

        def.Category.Should().Be("Physical");
    }

    [Fact]
    public void Create_ShouldCarryExplicitTacticalContract()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 3, 10,
            tacticalRange: 4,
            tacticalAreaShape: "Cross",
            requiresLineOfSight: true,
            cooldown: 2,
            isUltimate: true,
            emotionalRegister: "Effroi");

        def.TacticalRange.Should().Be(4);
        def.TacticalAreaShape.Should().Be("Cross");
        def.RequiresLineOfSight.Should().BeTrue();
        def.Cooldown.Should().Be(2);
        def.IsUltimate.Should().BeTrue();
        def.EmotionalRegister.Should().Be("effroi");
    }

    [Theory]
    [InlineData("Line")]
    [InlineData("")]
    [InlineData("Cone")]
    public void Create_ShouldThrow_WhenTacticalAreaShapeIsUnsupported(string shape)
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            tacticalAreaShape: shape);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition tactical area shape must be Single, Cross, Diamond or Map.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCooldownIsNegative()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            cooldown: -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition cooldown cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenEmotionalRegisterIsNotExplicit()
    {
        var act = () => SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Damage", "SingleEnemy", "Damage", 5, 0, 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill definition emotional register is required.");
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var def = SkillDefinition.Create(
            "skill.test", "Name", "Desc", "1.0.0", "Buff", "Self", "Buff", 2, 0, 0,
            emotionalRegister: "Neutral");

        def.Activate();

        def.Status.Should().Be(CatalogContentStatus.Active);
        def.IsActive.Should().BeTrue();
    }
}
