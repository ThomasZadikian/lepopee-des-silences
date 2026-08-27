using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class PalaceLawMapperCoverageTests
{
    public static TheoryData<string> SupportedEffects => new()
    {
        "AddStartingGuard",
        "ModifyDifficultyMultiplier",
        "ModifyRewardPowerMultiplier",
        "ModifyAttackPower",
        "ModifyDefense",
        "ModifySpeed",
        "EnableTurnOrderReverse",
        "EnableTurnOrderLock",
        "EnableRoomTraversalHpDrain",
        "EnableHitCounterDoubleDamage",
        "EnableMirrorCombatCopy",
        "EnableFirstHitCritical",
        "EnableLowHpDamageAmplification",
        "EnableDotDurationExtension",
        "EnableDuelDamageAsymmetry",
        "EnableCruelDestinyForEveryone",
        "EnableAllyHealingBonus",
        "EnableReputationChangeDoubled",
        "EnableSilenceDuActive",
        "EnableWoundHealingBlocked",
        "EnableConsumablesRestrictedInCombat",
        "EnablePostDeathBasicAttackOnly",
        "EnableTapisPropreEnabled",
        "EnableThirdCupHealCorruption",
        "EnableAbondanceExtraChoice",
        "EnablePresentations",
        "EnableMiroir",
        "EnableLootChanceBonus",
        "EnableSkillForgotten",
        "EnableRoomToll",
        "EnableCurrencyGainBonus",
        "EnableItemNodeReroll",
        "EnableSuspendSevereLaws",
        "EnableUpcomingRoomNamesReveal"
    };

    [Theory]
    [MemberData(nameof(SupportedEffects))]
    public void CreatePalaceLaw_ShouldMapEverySupportedRuntimeEffect(string effectType)
    {
        var definition = Definition(
            Effect(effectType, value: 2, condition: null));

        var act = () => PalaceLawMapper.CreatePalaceLaw(definition);

        act.Should().NotThrow();
        act().Effects.Should().ContainSingle();
    }

    [Theory]
    [InlineData("grey")]
    [InlineData("grisaille")]
    [InlineData("rain")]
    [InlineData("pluie")]
    [InlineData("heatwave")]
    [InlineData("canicule")]
    [InlineData("hail")]
    [InlineData("grele")]
    [InlineData("grêle")]
    [InlineData("brume")]
    [InlineData("voile")]
    [InlineData("orage")]
    [InlineData("accords")]
    [InlineData("pluie-de-cendres")]
    [InlineData("pluie de cendres")]
    [InlineData("deuil-sec")]
    [InlineData("pluie-violacee")]
    [InlineData("pluie violacee")]
    [InlineData("pluie-violacée")]
    [InlineData("pluie violacée")]
    [InlineData("maree-haute")]
    [InlineData("marée-haute")]
    [InlineData("accalmie")]
    [InlineData("repit")]
    [InlineData("répit")]
    public void CreatePalaceLaw_ShouldMapEverySupportedClimateAlias(string climate)
    {
        var definition = Definition(Effect("ApplyRoomClimate", value: 1, condition: climate));

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().ContainSingle();
    }

    [Fact]
    public void CreatePalaceLaw_ShouldIgnoreCatalogOnlyEffects()
    {
        var definition = Definition(
            Effect("ModifyGenerationWeight", 1),
            Effect("ModifyEnemyBehavior", 1),
            Effect("AddStartingGuard", 1));

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().ContainSingle();
    }

    [Fact]
    public void CreatePalaceLaw_ShouldUseBehaviorTagAsClimateFallback()
    {
        var effect = new CatalogEffectDefinitionSnapshot(
            "ApplyRoomClimate", "Run", 1, "Flat", "UntilRunEnds", "None",
            null, 0, "pluie", null, null);

        var law = PalaceLawMapper.CreatePalaceLaw(Definition(effect));

        law.Effects.Should().ContainSingle();
    }

    [Theory]
    [InlineData("Generation")]
    [InlineData("Events")]
    [InlineData("Combat")]
    [InlineData("Rewards")]
    [InlineData("Narrative")]
    [InlineData("HimLit")]
    public void CreatePalaceLaw_ShouldAcceptEveryImpactDomain(string domain)
    {
        var definition = Definition(Effect("AddStartingGuard", 1)) with
        {
            ImpactDomains = [domain, domain.ToLowerInvariant()]
        };

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Domains.Should().ContainSingle();
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectEmptyImpactDomains()
    {
        var definition = Definition(Effect("AddStartingGuard", 1)) with { ImpactDomains = [] };

        var act = () => PalaceLawMapper.CreatePalaceLaw(definition);

        act.Should().Throw<DomainException>().WithMessage("*must declare at least one impact domain*");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectUnknownImpactDomain()
    {
        var definition = Definition(Effect("AddStartingGuard", 1)) with { ImpactDomains = ["Unknown"] };

        var act = () => PalaceLawMapper.CreatePalaceLaw(definition);

        act.Should().Throw<DomainException>().WithMessage("*unsupported impact domain*");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectUnknownEffectType()
    {
        var act = () => PalaceLawMapper.CreatePalaceLaw(Definition(Effect("UnknownEffect", 1)));

        act.Should().Throw<DomainException>().WithMessage("*effect type 'UnknownEffect' is not supported*");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectKnownButUnsupportedRuntimeEffect()
    {
        var act = () => PalaceLawMapper.CreatePalaceLaw(Definition(Effect("HealVitality", 1)));

        act.Should().Throw<DomainException>().WithMessage("*effect type 'HealVitality' is not supported*");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectUnknownDuration()
    {
        var effect = new CatalogEffectDefinitionSnapshot(
            "AddStartingGuard", "Run", 1, "Flat", "Forever", "None",
            null, 0, null, null, null);

        var act = () => PalaceLawMapper.CreatePalaceLaw(Definition(effect));

        act.Should().Throw<DomainException>().WithMessage("*duration 'Forever' is not supported*");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldRejectUnsupportedClimate()
    {
        var act = () => PalaceLawMapper.CreatePalaceLaw(
            Definition(Effect("ApplyRoomClimate", 1, "snow")));

        act.Should().Throw<DomainException>().WithMessage("*supported climate condition*");
    }

    private static PalaceLawDefinitionSnapshot Definition(params CatalogEffectDefinitionSnapshot[] effects) =>
        new(
            "law.coverage",
            "Coverage law",
            "Coverage",
            "1.0",
            "Active",
            "Public",
            1,
            ["Combat"],
            Effects: effects,
            Rarity: "Commun",
            Polarity: "Neutre",
            ExclusionKeys: []);

    private static CatalogEffectDefinitionSnapshot Effect(
        string effectType,
        decimal value,
        string? condition = null) =>
        new(
            effectType,
            "Run",
            value,
            "Flat",
            "UntilRunEnds",
            "None",
            condition,
            0,
            null,
            null,
            null);
}
