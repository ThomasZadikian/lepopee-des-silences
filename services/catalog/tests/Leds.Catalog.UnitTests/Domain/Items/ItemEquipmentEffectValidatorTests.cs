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
            new(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Speed", Amount: 10),
            new(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.test"),
            new(ItemEquipmentEffectKind.DamageReductionByType, Amount: 15,
                AffinityRegister: EmotionalRegister.Memoire),
            new(ItemEquipmentEffectKind.AffinityOutcomeOverride,
                AffinityRegister: EmotionalRegister.Silence,
                AffinityOutcome: AffinityOutcome.Immune,
                Priority: 100),
            new(ItemEquipmentEffectKind.AffinityMultiplierPercent, Amount: -10,
                AffinityRegister: EmotionalRegister.Rupture,
                DurationActivations: 2),
            new(ItemEquipmentEffectKind.RuntimeBehavior,
                BehaviorCode: "reflect-first-melee-hit")
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectUnknownStatKind()
    {
        var effects = new[]
        {
            new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonus, StatKind: "Luck", Amount: 1)
        };

        var act = () => ItemEquipmentEffectValidator.Validate("item.test", effects);

        act.Should().Throw<DomainException>().WithMessage("*supported StatKind*");
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
}
