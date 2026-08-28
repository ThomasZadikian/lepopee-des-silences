using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatFactoryDeepBranchCoverageTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    [Fact]
    public void ScalarHelpers_ShouldCoverEverySwitchAndBooleanOutcome()
    {
        Invoke("EncounterBonus", "RoomBoss").Should().NotBeNull();
        Invoke("EncounterBonus", "Elite").Should().NotBeNull();
        Invoke("EncounterBonus", "Rare").Should().NotBeNull();
        Invoke("EncounterBonus", "Combat").Should().NotBeNull();
        ((bool)Invoke("IsDamageEffect", "Damage")!).Should().BeTrue();
        ((bool)Invoke("IsDamageEffect", "damagevitality")!).Should().BeTrue();
        ((bool)Invoke("IsDamageEffect", "Heal")!).Should().BeFalse();
        Invoke("ParseTacticalAreaShape", "single").Should().NotBeNull();
        Action invalidShape = () => Invoke("ParseTacticalAreaShape", "not-a-shape");
        invalidShape.Should().Throw<DomainException>();
    }

    [Fact]
    public void InitiativeLawHelpers_ShouldHandleEmptyUniformAndMixedRosters()
    {
        Invoke("ApplyTurnOrderReversal", (object)Array.Empty<Combatant>());
        Invoke("ApplyStrictInitiativeOrder", (object)Array.Empty<Combatant>());
        Invoke("ApplyCruelDestinyToEveryone", (object)Array.Empty<Combatant>());
        var uniform = new[] { CombatantWithSpeed("a", 10), CombatantWithSpeed("b", 10) };
        Invoke("ApplyTurnOrderReversal", (object)uniform);
        Invoke("ApplyStrictInitiativeOrder", (object)uniform);
        var reversed = new[] { CombatantWithSpeed("slow", 5), CombatantWithSpeed("middle", 10), CombatantWithSpeed("fast", 15) };
        Invoke("ApplyTurnOrderReversal", (object)reversed);
        reversed[0].EffectiveSpeed.Should().BeGreaterThan(reversed[2].EffectiveSpeed);
        var flattened = new[] { CombatantWithSpeed("slow-flat", 5), CombatantWithSpeed("middle-flat", 10), CombatantWithSpeed("fast-flat", 15) };
        Invoke("ApplyStrictInitiativeOrder", (object)flattened);
        flattened.Select(c => c.EffectiveSpeed).Distinct().Should().ContainSingle();
        var destined = CombatantWithSpeed("destined", 10);
        Invoke("ApplyCruelDestinyToEveryone", (object)new[] { destined });
        destined.StatusEffects.Should().Contain(effect => effect.Key == "law-destinee:dot");
    }

    [Fact]
    public void AttackTypeOverride_ShouldCoverMissingConsumedInvalidNeutralAndValidModifiers()
    {
        Invoke("ResolveAttackTypeOverride", (object)Array.Empty<RunModifier>()).Should().BeNull();
        var neutral = Modifier(RunModifierType.AttackTypeOverride, (double)EmotionalType.Neutral);
        Invoke("ResolveAttackTypeOverride", (object)new[] { neutral }).Should().BeNull();
        var invalid = Modifier(RunModifierType.AttackTypeOverride, 999);
        Invoke("ResolveAttackTypeOverride", (object)new[] { invalid }).Should().BeNull();
        var validType = Enum.GetValues<EmotionalType>().First(value => value != EmotionalType.Neutral);
        var valid = Modifier(RunModifierType.AttackTypeOverride, (double)validType);
        Invoke("ResolveAttackTypeOverride", (object)new[] { valid }).Should().Be(validType);
        valid.Consume(DateTime.UtcNow);
        Invoke("ResolveAttackTypeOverride", (object)new[] { valid }).Should().BeNull();
    }

    [Fact]
    public void ActiveClimateResolver_ShouldCoverNoMatchConsumedWrongRoomAllCanonicalValuesAndUnknownValue()
    {
        var roomId = Guid.NewGuid();
        Invoke("ResolveActiveClimate", roomId, Array.Empty<RunModifier>()).Should().BeNull();
        var wrongRoom = Modifier(RunModifierType.RoomClimate, 1, Guid.NewGuid());
        Invoke("ResolveActiveClimate", roomId, new[] { wrongRoom }).Should().BeNull();
        var consumed = Modifier(RunModifierType.RoomClimate, 1, roomId);
        consumed.Consume(DateTime.UtcNow);
        Invoke("ResolveActiveClimate", roomId, new[] { consumed }).Should().BeNull();
        for (var value = 1; value <= 9; value++)
            Invoke("ResolveActiveClimate", roomId, new[] { Modifier(RunModifierType.RoomClimate, value, roomId) }).Should().NotBeNull();
        Invoke("ResolveActiveClimate", roomId, new[] { Modifier(RunModifierType.RoomClimate, 999, roomId) }).Should().BeNull();
    }

    [Fact]
    public void ClimateStatBundle_ShouldCoverNoClimateIgnoredClimateEveryAuthoredBundleAndEmptyRoster()
    {
        var climateType = ClimateType();
        var combatant = CombatantWithSpeed("climate", 10);
        Invoke("ApplyClimateStatBundle", null, new[] { combatant });
        Invoke("ApplyClimateStatBundle", Enum.Parse(climateType, "Grey"), new[] { combatant });
        Invoke("ApplyClimateStatBundle", Enum.Parse(climateType, "Brume"), Array.Empty<Combatant>());
        foreach (var name in new[] { "Brume", "Orage", "PluieDeCendres", "PluieViolacee" })
            Invoke("ApplyClimateStatBundle", Enum.Parse(climateType, name), new[] { combatant });
        combatant.StatusEffects.Should().Contain(effect => effect.Key == "climat-brume:focus");
        combatant.StatusEffects.Should().Contain(effect => effect.Key == "climat-orage:magic-damage");
        combatant.StatusEffects.Should().Contain(effect => effect.Key == "climat-pluie-de-cendres:healing");
        combatant.StatusEffects.Should().Contain(effect => effect.Key == "climat-pluie-violacee:periodic-damage");
    }

    [Fact]
    public void AllyHealingAndSilenceBundles_ShouldCoverInactiveConsumedActiveAndEmptyRosters()
    {
        var ally = CombatantWithSpeed("ally", 10);
        Invoke("ApplyAllyHealingBonus", Array.Empty<RunModifier>(), new[] { ally });
        var consumedHealing = Modifier(RunModifierType.AllyHealingBonus, 20);
        consumedHealing.Consume(DateTime.UtcNow);
        Invoke("ApplyAllyHealingBonus", new[] { consumedHealing }, new[] { ally });
        Invoke("ApplyAllyHealingBonus", new[] { Modifier(RunModifierType.AllyHealingBonus, 20) }, Array.Empty<Combatant>());
        Invoke("ApplyAllyHealingBonus", new[] { Modifier(RunModifierType.AllyHealingBonus, 20) }, new[] { ally });
        ally.StatusEffects.Should().Contain(effect => effect.Stat == CombatStat.HealingBonus);
        Invoke("ApplySilenceDuBundle", Array.Empty<RunModifier>(), new[] { ally });
        var consumedSilence = Modifier(RunModifierType.SilenceDuActive, 1);
        consumedSilence.Consume(DateTime.UtcNow);
        Invoke("ApplySilenceDuBundle", new[] { consumedSilence }, new[] { ally });
        Invoke("ApplySilenceDuBundle", new[] { Modifier(RunModifierType.SilenceDuActive, 1) }, Array.Empty<Combatant>());
        Invoke("ApplySilenceDuBundle", new[] { Modifier(RunModifierType.SilenceDuActive, 1) }, new[] { ally });
        ally.StatusEffects.Should().Contain(effect => effect.Stat == CombatStat.FlatManaCostBonus);
    }

    [Fact]
    public void RemainingScalarHelpers_ShouldCoverPositiveNegativeAndShortCircuitPaths()
    {
        ((int)Invoke("ComputeDotDurationExtensionTicks", (object)Array.Empty<RunModifier>())!).Should().Be(0);
        var dot = Modifier(RunModifierType.DotDurationExtension, 2);
        ((int)Invoke("ComputeDotDurationExtensionTicks", (object)new[] { dot })!).Should().BeGreaterThan(0);
        dot.Consume(DateTime.UtcNow);
        ((int)Invoke("ComputeDotDurationExtensionTicks", (object)new[] { dot })!).Should().Be(0);

        ((bool)Invoke("ComputeDuelDamageAsymmetryEnabled", (object)Array.Empty<RunModifier>())!).Should().BeFalse();
        var duel = Modifier(RunModifierType.DuelDamageAsymmetry, 1);
        ((bool)Invoke("ComputeDuelDamageAsymmetryEnabled", (object)new[] { duel })!).Should().BeTrue();
        duel.Consume(DateTime.UtcNow);
        ((bool)Invoke("ComputeDuelDamageAsymmetryEnabled", (object)new[] { duel })!).Should().BeFalse();

        Invoke("NormalizeCombatEffectType", "skill.basic.guard", "Damage").Should().Be("Guard");
        Invoke("NormalizeCombatEffectType", "skill.other", "AddCurrentGuard").Should().Be("Guard");
        Invoke("NormalizeCombatEffectType", "skill.other", "Damage").Should().Be("Damage");

        ((int)Invoke("ApplySpeedMultiplier", 10, 1.0)!).Should().Be(10);
        ((int)Invoke("ApplySpeedMultiplier", 10, 1.5)!).Should().Be(15);
        ((int)Invoke("ApplySpeedMultiplier", 0, 0.5)!).Should().Be(1);

        ((int)Invoke("ScalePlayerSkillPower", "Heal", 12, 2.0)!).Should().Be(12);
        ((int)Invoke("ScalePlayerSkillPower", "Damage", 12, 2.0)!).Should().Be(24);
        ((int)Invoke("ScalePlayerSkillPower", "DamageVitality", 12, 2.0)!).Should().Be(24);

        ((int)Invoke("ScaleEnemySkillPower", "Damage", 10, 1.0, PalaceRoomState.Neutral)!).Should().Be(10);
        ((int)Invoke("ScaleEnemySkillPower", "Damage", 10, 1.0, PalaceRoomState.Painful)!).Should().Be(9);
        ((int)Invoke("ScaleEnemySkillPower", "Heal", 10, 1.0, PalaceRoomState.Painful)!).Should().Be(10);
    }

    [Fact]
    public void EquipmentConditionHelpers_ShouldCoverRoomWeatherNullUnknownAndScalarAggregationPaths()
    {
        var climateType = ClimateType();
        var rain = Enum.Parse(climateType, "Rain");

        ((bool)Invoke("MatchesEquipmentCondition", null, "Montagne", rain)!).Should().BeFalse();
        ((bool)Invoke("MatchesEquipmentCondition", "room:Montagne", "Montagne", rain)!).Should().BeTrue();
        ((bool)Invoke("MatchesEquipmentCondition", "room:Montagne", "Jardin", rain)!).Should().BeFalse();
        ((bool)Invoke("MatchesEquipmentCondition", "weather:Rain", "Montagne", null)!).Should().BeFalse();
        ((bool)Invoke("MatchesEquipmentCondition", "weather:Rain", "Montagne", rain)!).Should().BeTrue();
        ((bool)Invoke("MatchesEquipmentCondition", "weather:Hail", "Montagne", rain)!).Should().BeFalse();
        ((bool)Invoke("MatchesEquipmentCondition", "other:value", "Montagne", rain)!).Should().BeFalse();

        var effects = new CatalogItemEquipmentEffect[]
        {
            Effect("StatBonus", "Mana", 5, "room:Montagne"),
            Effect("StatBonusPercent", "Mana", 50, "room:Montagne"),
            Effect("StatBonus", "Speed", 99, "room:Montagne"),
            Effect("StatBonus", "Mana", null, "room:Montagne"),
            Effect("StatBonus", "Mana", 99, "room:Jardin")
        };
        ((int)Invoke("AdjustConditionalScalarStat", 10, "Mana", "Montagne", null, effects, 0)!).Should().Be(20);
        ((int)Invoke("AdjustConditionalScalarStat", 0, "Mana", "Montagne", null, effects, 3)!).Should().Be(5);
    }

    [Fact]
    public void ConditionalEquipmentBundle_ShouldCoverEveryValidationContinueAndApplicationPath()
    {
        var actor = CombatantWithSpeed("conditional", 10);
        Invoke("ApplyConditionalEquipmentStatBundle", null, "Montagne", null, Array.Empty<CatalogItemEquipmentEffect>());
        Invoke("ApplyConditionalEquipmentStatBundle", actor, "Montagne", null, Array.Empty<CatalogItemEquipmentEffect>());

        var effects = new CatalogItemEquipmentEffect[]
        {
            Effect("StatBonus", null, 5, "room:Montagne"),
            Effect("StatBonus", "Speed", null, "room:Montagne"),
            Effect("StatBonus", "UnknownStat", 5, "room:Montagne"),
            Effect("UnknownKind", "Speed", 5, "room:Montagne"),
            Effect("StatBonus", "Speed", 5, "room:Jardin"),
            Effect("StatBonusPercent", "Speed", 10, "room:Montagne"),
            Effect("StatBonus", "Defense", 5, "room:Montagne")
        };

        Invoke("ApplyConditionalEquipmentStatBundle", actor, "Montagne", null, effects);
        actor.StatusEffects.Should().Contain(effect => effect.Key.Contains("conditional-equip", StringComparison.Ordinal));
    }

    [Fact]
    public void EquipmentAffinityAndRuntimeHelpers_ShouldCoverIgnoredValidAndInvalidPaths()
    {
        var actor = CombatantWithSpeed("equipment", 10);
        Invoke("ApplyEquipmentAffinityModifiers", null, Array.Empty<CatalogItemEquipmentEffect>());
        Invoke("ApplyEquipmentAffinityModifiers", actor, new[] { Effect("StatBonus", "Speed", 5, "room:Montagne") });

        var outcome = Enum.GetValues<DamageEffectiveness>().First().ToString();
        Invoke("ApplyEquipmentAffinityModifiers", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "AffinityOutcomeOverride", null, null, null, "Memoire",
                AffinityOutcome: outcome, Priority: 2, DurationActivations: 1,
                SourceDefinitionKey: "item.affinity.outcome"),
            new CatalogItemEquipmentEffect(
                "AffinityMultiplierPercent", null, 25, null, "Memoire",
                AffinityOutcome: null, Priority: 1,
                SourceDefinitionKey: "item.affinity.multiplier")
        });

        Action invalidOutcome = () => Invoke("ApplyEquipmentAffinityModifiers", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "AffinityOutcomeOverride", null, null, null, "Memoire",
                AffinityOutcome: "not-an-outcome", SourceDefinitionKey: "item.invalid")
        });
        invalidOutcome.Should().Throw<DomainException>();

        Action missingSource = () => Invoke("ApplyEquipmentAffinityModifiers", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "AffinityMultiplierPercent", null, 10, null, "Memoire")
        });
        missingSource.Should().Throw<DomainException>();

        Invoke("ApplyEquipmentRuntimeBehaviors", null, Array.Empty<CatalogItemEquipmentEffect>());
        Invoke("ApplyEquipmentRuntimeBehaviors", actor, new[] { Effect("StatBonus", "Speed", 5, "room:Montagne") });
        Invoke("ApplyEquipmentRuntimeBehaviors", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "RuntimeBehavior", null, null, null, null,
                BehaviorCode: "reflect-first-melee-hit", SourceDefinitionKey: "item.runtime")
        });

        Action missingBehavior = () => Invoke("ApplyEquipmentRuntimeBehaviors", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "RuntimeBehavior", null, null, null, null,
                SourceDefinitionKey: "item.runtime")
        });
        missingBehavior.Should().Throw<DomainException>();

        Action missingRuntimeSource = () => Invoke("ApplyEquipmentRuntimeBehaviors", actor, new[]
        {
            new CatalogItemEquipmentEffect(
                "RuntimeBehavior", null, null, null, null,
                BehaviorCode: "reflect-first-melee-hit")
        });
        missingRuntimeSource.Should().Throw<DomainException>();
    }

    private static object? Invoke(string name, params object?[] arguments)
    {
        var candidates = typeof(CombatFactory).GetMethods(PrivateStatic)
            .Where(method => method.Name == name && method.GetParameters().Length == arguments.Length).ToArray();
        candidates.Should().ContainSingle($"private helper {name} should be unambiguous");
        try { return candidates[0].Invoke(null, arguments); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { throw exception.InnerException; }
    }

    private static Type ClimateType() =>
        typeof(CombatFactory).GetNestedType("RoomClimate", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("RoomClimate enum was not found.");

    private static CatalogItemEquipmentEffect Effect(string kind, string? stat, int? amount, string? condition) =>
        new(kind, stat, amount, null, null, Condition: condition, SourceDefinitionKey: $"test.{Guid.NewGuid():N}");

    private static RunModifier Modifier(RunModifierType type, double value, Guid? roomId = null) =>
        RunModifier.Create(type, value, RunModifierDuration.UntilRoomEnds, "test", $"test.{type}.{Guid.NewGuid():N}", expiresAtRoomId: roomId);

    private static Combatant CombatantWithSpeed(string key, int speed) =>
        Combatant.CreateEnemy($"test.{key}", key, "Test", 100, speed: speed, defense: 10, magicDefense: 10, mana: 10);
}
