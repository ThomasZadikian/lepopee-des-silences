using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
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
        Invoke("ApplyTurnOrderReversal", Array.Empty<Combatant>());
        Invoke("ApplyStrictInitiativeOrder", Array.Empty<Combatant>());
        Invoke("ApplyCruelDestinyToEveryone", Array.Empty<Combatant>());
        var uniform = new[] { CombatantWithSpeed("a", 10), CombatantWithSpeed("b", 10) };
        Invoke("ApplyTurnOrderReversal", uniform);
        Invoke("ApplyStrictInitiativeOrder", uniform);
        var reversed = new[] { CombatantWithSpeed("slow", 5), CombatantWithSpeed("middle", 10), CombatantWithSpeed("fast", 15) };
        Invoke("ApplyTurnOrderReversal", reversed);
        reversed[0].EffectiveSpeed.Should().BeGreaterThan(reversed[2].EffectiveSpeed);
        var flattened = new[] { CombatantWithSpeed("slow-flat", 5), CombatantWithSpeed("middle-flat", 10), CombatantWithSpeed("fast-flat", 15) };
        Invoke("ApplyStrictInitiativeOrder", flattened);
        flattened.Select(c => c.EffectiveSpeed).Distinct().Should().ContainSingle();
        var destined = CombatantWithSpeed("destined", 10);
        Invoke("ApplyCruelDestinyToEveryone", new[] { destined });
        destined.StatusEffects.Should().Contain(effect => effect.Key == "law-destinee:dot");
    }

    [Fact]
    public void AttackTypeOverride_ShouldCoverMissingConsumedInvalidNeutralAndValidModifiers()
    {
        Invoke("ResolveAttackTypeOverride", Array.Empty<RunModifier>()).Should().BeNull();
        var neutral = Modifier(RunModifierType.AttackTypeOverride, (double)EmotionalType.Neutral);
        Invoke("ResolveAttackTypeOverride", new[] { neutral }).Should().BeNull();
        var invalid = Modifier(RunModifierType.AttackTypeOverride, 999);
        Invoke("ResolveAttackTypeOverride", new[] { invalid }).Should().BeNull();
        var validType = Enum.GetValues<EmotionalType>().First(value => value != EmotionalType.Neutral);
        var valid = Modifier(RunModifierType.AttackTypeOverride, (double)validType);
        Invoke("ResolveAttackTypeOverride", new[] { valid }).Should().Be(validType);
        valid.Consume(DateTime.UtcNow);
        Invoke("ResolveAttackTypeOverride", new[] { valid }).Should().BeNull();
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
        var climateType = typeof(CombatFactory).GetNestedType("RoomClimate", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RoomClimate enum was not found.");
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

    private static object? Invoke(string name, params object?[] arguments)
    {
        var candidates = typeof(CombatFactory).GetMethods(PrivateStatic)
            .Where(method => method.Name == name && method.GetParameters().Length == arguments.Length).ToArray();
        candidates.Should().ContainSingle($"private helper {name} should be unambiguous");
        try { return candidates[0].Invoke(null, arguments); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { throw exception.InnerException; }
    }

    private static RunModifier Modifier(RunModifierType type, double value, Guid? roomId = null) =>
        RunModifier.Create(type, value, RunModifierDuration.UntilRoomEnds, "test", $"test.{type}.{Guid.NewGuid():N}", expiresAtRoomId: roomId);

    private static Combatant CombatantWithSpeed(string key, int speed) =>
        Combatant.CreateEnemy($"test.{key}", key, "Test", 100, speed: speed, defense: 10, magicDefense: 10, mana: 10);
}
