using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// <see cref="PalaceLawMapper"/> — the shared PalaceLawDefinitionSnapshot → PalaceLaw
/// construction previously duplicated across the (now-retired) Law node choice resolver,
/// NpcConsequenceApplier, and DevToolsRunDebugService.
/// </summary>
public sealed class PalaceLawMapperTests
{
    private static PalaceLawDefinitionSnapshot CreateDefinition(
        IReadOnlyCollection<string>? impactDomains = null,
        IReadOnlyCollection<CatalogEffectDefinitionSnapshot>? effects = null,
        string rarity = "Rare",
        string polarity = "Sévère",
        bool isMajeure = true,
        string? roomKey = "room08",
        bool isCumulExempt = true,
        IReadOnlyCollection<string>? exclusionKeys = null) => new(
        Key: "law-test-v1",
        Name: "Loi de test",
        Description: "desc",
        Version: "1.0.0",
        Status: "Active",
        Visibility: "Public",
        Priority: 0,
        ImpactDomains: impactDomains ?? [],
        BaseWeight: 1,
        Rarity: rarity,
        Polarity: polarity,
        IsMajeure: isMajeure,
        RoomKey: roomKey,
        IsCumulExempt: isCumulExempt,
        ExclusionKeys: exclusionKeys ?? ["law-other-v1"],
        Effects: effects);

    [Fact]
    public void CreatePalaceLaw_ShouldThreadPromulgationMetadata()
    {
        var definition = CreateDefinition(impactDomains: ["Combat"]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Rarity.Should().Be("Rare");
        law.Polarity.Should().Be("Sévère");
        law.IsMajeure.Should().BeTrue();
        law.RoomKey.Should().Be("room08");
        law.IsCumulExempt.Should().BeTrue();
        law.ExclusionKeys.Should().Contain("law-other-v1");
    }

    [Fact]
    public void CreatePalaceLaw_ShouldUseFallbackDomain_WhenNoImpactDomainParses()
    {
        var definition = CreateDefinition(impactDomains: ["not-a-real-domain"]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition, PalaceLawDomain.Combat);

        law.Domains.Should().ContainSingle().Which.Should().Be(PalaceLawDomain.Combat);
    }

    [Fact]
    public void CreatePalaceLaw_ShouldDefaultFallbackDomainToNarrative()
    {
        var definition = CreateDefinition(impactDomains: []);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Domains.Should().ContainSingle().Which.Should().Be(PalaceLawDomain.Narrative);
    }

    [Fact]
    public void CreatePalaceLaw_ShouldMapKnownEffectTypes()
    {
        var definition = CreateDefinition(
            impactDomains: ["Combat"],
            effects:
            [
                new CatalogEffectDefinitionSnapshot(
                    "ModifyAttackPower", "Run", 5m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null),
            ]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().ContainSingle(effect => effect.ModifierType == RunModifierType.AttackPowerBonus);
    }

    [Fact]
    public void CreatePalaceLaw_ShouldSkipGenerationAndBehaviorOnlyEffects()
    {
        var definition = CreateDefinition(
            impactDomains: ["Combat"],
            effects:
            [
                new CatalogEffectDefinitionSnapshot(
                    "ModifyGenerationWeight", "Run", 1m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null),
                new CatalogEffectDefinitionSnapshot(
                    "ModifyEnemyBehavior", "Run", 1m, "Flat", "UntilRunEnds", "Additive", null, 1, null, null, null),
            ]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().BeEmpty(
            because: "generation-weight and enemy-behavior effects have no direct RunModifier translation.");
    }

    [Theory]
    [InlineData("EnableTurnOrderReverse", RunModifierType.TurnOrderReverse)]
    [InlineData("EnableTurnOrderLock", RunModifierType.TurnOrderLock)]
    [InlineData("EnableRoomTraversalHpDrain", RunModifierType.RoomTraversalHpDrain)]
    [InlineData("EnableHitCounterDoubleDamage", RunModifierType.HitCounterDoubleDamage)]
    [InlineData("EnableMirrorCombatCopy", RunModifierType.MirrorCombatCopy)]
    [InlineData("EnableFirstHitCritical", RunModifierType.FirstHitCritical)]
    [InlineData("EnableLowHpDamageAmplification", RunModifierType.DamageAmplificationBelowHpThreshold)]
    [InlineData("EnableCruelDestinyForEveryone", RunModifierType.CruelDestinyForEveryone)]
    [InlineData("EnableAllyHealingBonus", RunModifierType.AllyHealingBonus)]
    [InlineData("EnableReputationChangeDoubled", RunModifierType.ReputationChangeDoubled)]
    [InlineData("EnableSilenceDuActive", RunModifierType.SilenceDuActive)]
    [InlineData("EnableWoundHealingBlocked", RunModifierType.WoundHealingBlocked)]
    [InlineData("EnableConsumablesRestrictedInCombat", RunModifierType.ConsumablesRestrictedInCombat)]
    public void CreatePalaceLaw_ShouldMapExoticMechanicGateEffects(string effectType, RunModifierType expected)
    {
        var definition = CreateDefinition(
            impactDomains: ["Combat"],
            effects:
            [
                new CatalogEffectDefinitionSnapshot(
                    effectType, "Run", 1m, "Flat", "UntilRoomEnds", "Additive", null, 0, null, null, null),
            ]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().ContainSingle(effect => effect.ModifierType == expected);
    }

    [Fact]
    public void CreatePalaceLaw_ShouldMapDuelDamageAsymmetryGateEffect()
    {
        var definition = CreateDefinition(
            impactDomains: ["Combat"],
            effects:
            [
                new CatalogEffectDefinitionSnapshot(
                    "EnableDuelDamageAsymmetry", "Run", 1m, "Flat", "UntilRoomEnds", "Additive", null, 0, null, null, null),
            ]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        law.Effects.Should().ContainSingle(effect => effect.ModifierType == RunModifierType.DuelDamageAsymmetry);
    }

    [Fact]
    public void CreatePalaceLaw_ShouldMapDotDurationExtension_PreservingItsMagnitude()
    {
        var definition = CreateDefinition(
            impactDomains: ["Combat"],
            effects:
            [
                new CatalogEffectDefinitionSnapshot(
                    "EnableDotDurationExtension", "Run", 2m, "Flat", "UntilRoomEnds", "Additive", null, 0, null, null, null),
            ]);

        var law = PalaceLawMapper.CreatePalaceLaw(definition);

        var effect = law.Effects.Should().ContainSingle(e => e.ModifierType == RunModifierType.DotDurationExtension).Subject;
        effect.Value.Should().Be(2);
    }
}
