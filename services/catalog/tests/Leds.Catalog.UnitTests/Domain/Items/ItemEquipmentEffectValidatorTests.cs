using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemEquipmentEffectValidatorTests
{
    [Fact]
    public void Validate_ShouldAcceptEverySupportedDeclarativeShape()
    {
        var effects = new ItemEquipmentEffect[]
        {
            new(ItemEquipmentEffectKind.StatBonus, StatKind: "AttackPower", Amount: 1, Condition: "room:Hall"),
            new(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Speed", Amount: 10, Condition: "weather:Rain"),
            new(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.test"),
            new(ItemEquipmentEffectKind.GrantAffinity, AffinityRegister: EmotionalRegister.Memoire),
            new(ItemEquipmentEffectKind.DamageReductionByType, Amount: 15, AffinityRegister: EmotionalRegister.Memoire),
            new(ItemEquipmentEffectKind.DotDurationReduction, Amount: 0),
            new(ItemEquipmentEffectKind.DotDamageReduction, Amount: 100),
            new(ItemEquipmentEffectKind.MagicDamageReductionPercent, Amount: 50),
            new(ItemEquipmentEffectKind.HitChanceBonus, Amount: 5),
            new(ItemEquipmentEffectKind.MagicDamageBonusPercent, Amount: -5),
            new(ItemEquipmentEffectKind.CriticalChanceBonusPercent, Amount: 5),
            new(ItemEquipmentEffectKind.DotDamageBonusPercent, Amount: 5),
            new(ItemEquipmentEffectKind.HealingBonusPercent, Amount: 5),
            new(ItemEquipmentEffectKind.AffinityOutcomeOverride,
                AffinityRegister: EmotionalRegister.Silence,
                AffinityOutcome: AffinityOutcome.Immune,
                Priority: 100,
                DurationActivations: 2),
            new(ItemEquipmentEffectKind.AffinityMultiplierPercent, Amount: -10,
                AffinityRegister: EmotionalRegister.Rupture,
                DurationActivations: 2),
            new(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "reflect-first-melee-hit")
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectNullEffects()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_ShouldRejectConditionOnUnsupportedEffect()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.test", Condition: "room:Hall")
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*does not support Condition*");
    }

    [Theory]
    [InlineData("Luck")]
    [InlineData("")]
    public void Validate_ShouldRejectUnknownOrMissingStatKind(string statKind)
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonus, StatKind: statKind, Amount: 1)
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*supported StatKind*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingAmountForStatBonus()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonus, StatKind: "Speed")
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*requires Amount*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_ShouldRejectPercentageOutsideRange(int amount)
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.DotDamageReduction, Amount: amount)
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*between 0 and 100*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingPercentage()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.MagicDamageReductionPercent)
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*between 0 and 100*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingSkillKey()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: " ")]);

        act.Should().Throw<DomainException>().WithMessage("*requires SkillKey*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingGrantAffinityRegister()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantAffinity)]);

        act.Should().Throw<DomainException>().WithMessage("*requires AffinityRegister*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingDamageReductionAffinityRegister()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.DamageReductionByType, Amount: 10)]);

        act.Should().Throw<DomainException>().WithMessage("*requires AffinityRegister*");
    }

    [Fact]
    public void Validate_ShouldRejectIncompleteAffinityOverride()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.AffinityOutcomeOverride,
                AffinityRegister: EmotionalRegister.Folie)
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*AffinityOutcome*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingAffinityMultiplierRegister()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.AffinityMultiplierPercent, Amount: 10)]);

        act.Should().Throw<DomainException>().WithMessage("*requires AffinityRegister*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldRejectNonPositiveDurationWhenSupplied(int duration)
    {
        var effect = new ItemEquipmentEffect(
            ItemEquipmentEffectKind.AffinityOutcomeOverride,
            AffinityRegister: EmotionalRegister.Folie,
            AffinityOutcome: AffinityOutcome.Resistant,
            DurationActivations: duration);

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", [effect]);

        act.Should().Throw<DomainException>().WithMessage("*DurationActivations must be positive*");
    }

    [Theory]
    [InlineData("invalid:Hall")]
    [InlineData("room:")]
    [InlineData("weather:")]
    public void Validate_ShouldRejectMalformedCondition(string condition)
    {
        var effect = new ItemEquipmentEffect(
            ItemEquipmentEffectKind.StatBonus,
            StatKind: "Speed",
            Amount: 1,
            Condition: condition);

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", [effect]);

        act.Should().Throw<DomainException>().WithMessage("*condition*");
    }

    [Fact]
    public void Validate_ShouldRejectUnknownRuntimeBehavior()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(
                ItemEquipmentEffectKind.RuntimeBehavior,
                BehaviorCode: "unregistered-handler")
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*supported BehaviorCode*");
    }

    [Fact]
    public void Validate_ShouldRejectMissingRuntimeBehavior()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: " ")]);

        act.Should().Throw<DomainException>().WithMessage("*supported BehaviorCode*");
    }

    [Fact]
    public void Validate_ShouldRejectUnsupportedEnumValue()
    {
        var act = () => ItemEquipmentEffectValidator.Validate("item.test",
            [new ItemEquipmentEffect((ItemEquipmentEffectKind)999)]);

        act.Should().Throw<DomainException>().WithMessage("*not supported by the equipment runtime*");
    }
}
