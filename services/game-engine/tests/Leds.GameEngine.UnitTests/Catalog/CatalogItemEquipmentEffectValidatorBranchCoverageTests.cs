using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogItemEquipmentEffectValidatorBranchCoverageTests
{
    [Fact]
    public void Validate_ShouldAcceptCanonicalEffectFamilies()
    {
        var effects = new CatalogItemEquipmentEffect[]
        {
            E("StatBonus", stat: "AttackPower", amount: 2),
            E("StatBonusPercent", stat: "Speed", amount: 5, condition: "room:Forest"),
            E("GrantSkill", skill: "skill.test"),
            E("GrantAffinity", affinity: "Mémoire"),
            E("DamageReductionByType", amount: 20, affinity: "Rupture"),
            E("HitChanceBonus", amount: 10),
            E("DotDurationReduction", amount: 15),
            E("DotDamageReduction", amount: 15),
            E("MagicDamageBonusPercent", amount: 10),
            E("MagicDamageReductionPercent", amount: 10),
            E("CriticalChanceBonusPercent", amount: 5),
            E("DotDamageBonusPercent", amount: 5),
            E("HealingBonusPercent", amount: 10),
            E("AffinityOutcomeOverride", affinity: "Silence", outcome: "Resistant"),
            E("AffinityMultiplierPercent", amount: 10, affinity: "Folie"),
            E("RuntimeBehavior", behavior: "infinite-chalice")
        };

        FluentActions.Invoking(() => CatalogItemEquipmentEffectValidator.Validate("item.test", effects))
            .Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectUnsupportedKindAndConditionPlacement()
    {
        AssertInvalid(E("unknown"), "unsupported equipment effect");
        AssertInvalid(E("HitChanceBonus", amount: 1, condition: "room:Forest"), "does not support Condition");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public void Validate_ShouldRejectMissingOrUnsupportedStatKind(string? stat)
    {
        AssertInvalid(E("StatBonus", stat: stat, amount: 1), "unsupported StatKind");
    }

    [Theory]
    [InlineData("room:")]
    [InlineData("weather:")]
    [InlineData("foo:bar")]
    [InlineData("plain")]
    public void Validate_ShouldRejectMalformedConditions(string condition)
    {
        AssertInvalid(E("StatBonus", stat: "Defense", amount: 1, condition: condition), "condition must be");
    }

    [Fact]
    public void Validate_ShouldAcceptWeatherCondition()
    {
        FluentActions.Invoking(() => CatalogItemEquipmentEffectValidator.Validate(
            "item.test", [E("StatBonus", stat: "Defense", amount: 1, condition: "weather:Pluie")]))
            .Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectGrantSkillWithoutKey()
    {
        AssertInvalid(E("GrantSkill"), "requires SkillKey");
    }

    [Theory]
    [InlineData("GrantAffinity")]
    [InlineData("DamageReductionByType")]
    [InlineData("AffinityMultiplierPercent")]
    public void Validate_ShouldRejectAffinityEffectsWithoutRegister(string kind)
    {
        AssertInvalid(E(kind, amount: 10), "AffinityRegister");
    }

    [Fact]
    public void Validate_ShouldRejectInvalidAffinityOutcome()
    {
        AssertInvalid(E("AffinityOutcomeOverride", affinity: "Silence", outcome: "nope"), "affinity outcome is invalid");
    }

    [Theory]
    [InlineData("HitChanceBonus")]
    [InlineData("MagicDamageBonusPercent")]
    [InlineData("CriticalChanceBonusPercent")]
    [InlineData("DotDamageBonusPercent")]
    [InlineData("HealingBonusPercent")]
    public void Validate_ShouldRequireAmountForBonusFamilies(string kind)
    {
        AssertInvalid(E(kind), "requires Amount");
    }

    [Theory]
    [InlineData("DamageReductionByType", -1)]
    [InlineData("DamageReductionByType", 101)]
    [InlineData("DotDurationReduction", -1)]
    [InlineData("DotDamageReduction", 101)]
    [InlineData("MagicDamageReductionPercent", 101)]
    public void Validate_ShouldBoundReductionAmounts(string kind, int amount)
    {
        var affinity = kind == "DamageReductionByType" ? "Mémoire" : null;
        AssertInvalid(E(kind, amount: amount, affinity: affinity), "between 0 and 100");
    }

    [Fact]
    public void Validate_ShouldRequireReductionAmount()
    {
        AssertInvalid(E("DotDurationReduction"), "requires Amount");
    }

    [Fact]
    public void Validate_ShouldRejectNonPositiveAffinityDuration()
    {
        AssertInvalid(E("GrantAffinity", affinity: "Mémoire", duration: 0), "duration must be positive");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-handler")]
    public void Validate_ShouldRejectMissingOrUnknownRuntimeBehavior(string? behavior)
    {
        AssertInvalid(E("RuntimeBehavior", behavior: behavior), "runtime behavior code is not supported");
    }

    private static CatalogItemEquipmentEffect E(
        string kind,
        string? stat = null,
        int? amount = null,
        string? skill = null,
        string? affinity = null,
        string? condition = null,
        string? outcome = null,
        int? duration = null,
        string? behavior = null) =>
        new(kind, stat, amount, skill, affinity, condition, outcome,
            DurationActivations: duration, BehaviorCode: behavior);

    private static void AssertInvalid(CatalogItemEquipmentEffect effect, string message) =>
        FluentActions.Invoking(() => CatalogItemEquipmentEffectValidator.Validate("item.invalid", [effect]))
            .Should().Throw<DomainException>().WithMessage($"*{message}*");
}
