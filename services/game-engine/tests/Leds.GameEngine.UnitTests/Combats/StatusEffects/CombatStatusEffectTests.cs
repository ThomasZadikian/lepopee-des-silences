using FluentAssertions;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats.StatusEffects;

public sealed class CombatStatusEffectTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var effect = CombatStatusEffect.Create(
            "poison",
            "Poison",
            StatusEffectKind.DamageOverTime,
            currentTick: 0,
            durationTicks: 5000,
            magnitude: 10,
            stacks: 1,
            tickInterval: 1400);

        effect.Key.Should().Be("poison");
        effect.DisplayName.Should().Be("Poison");
        effect.Kind.Should().Be(StatusEffectKind.DamageOverTime);
        effect.Magnitude.Should().Be(10);
        effect.Stacks.Should().Be(1);
        effect.IsPeriodic.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldThrow_WhenKeyIsNullOrWhitespace()
    {
        var act1 = () => CombatStatusEffect.Create("", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);
        var act2 = () => CombatStatusEffect.Create("   ", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        act1.Should().Throw<DomainException>().WithMessage("Status effect key is required.");
        act2.Should().Throw<DomainException>().WithMessage("Status effect key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDurationIsZeroOrNegative()
    {
        var act1 = () => CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 0);
        var act2 = () => CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, -100);

        act1.Should().Throw<DomainException>().WithMessage("Status effect duration must be positive.");
        act2.Should().Throw<DomainException>().WithMessage("Status effect duration must be positive.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenStacksIsLessThanOne()
    {
        var act = () => CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000, stacks: 0);

        act.Should().Throw<DomainException>().WithMessage("Status effect must have at least one stack.");
    }

    [Fact]
    public void Create_ShouldTrimKey()
    {
        var effect = CombatStatusEffect.Create("  poison  ", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.Key.Should().Be("poison");
    }

    [Fact]
    public void Create_ShouldUseKeyAsDisplayName_WhenDisplayNameIsEmpty()
    {
        var effect = CombatStatusEffect.Create("poison", "", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.DisplayName.Should().Be("poison");
    }

    [Fact]
    public void Create_ShouldFloorTickInterval_ToMinimum()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 5000,
            tickInterval: 100);

        effect.TickInterval.Should().Be(1400);
    }

    [Fact]
    public void Create_ShouldSetNextTickAtTick_ForPeriodicEffects()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 100, durationTicks: 5000,
            tickInterval: 2000);

        effect.NextTickAtTick.Should().Be(100 + 2000);
    }

    [Fact]
    public void Create_ShouldSetNextTickAtIntMax_ForNonPeriodicEffects()
    {
        var effect = CombatStatusEffect.Create(
            "stun", "Stun", StatusEffectKind.Stun,
            currentTick: 100, durationTicks: 3000);

        effect.NextTickAtTick.Should().Be(int.MaxValue);
        effect.IsPeriodic.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetExpiresAtTick_Correctly()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 100, durationTicks: 5000);

        effect.ExpiresAtTick.Should().Be(5100);
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenCurrentTickAtOrPastExpiry()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.IsExpired(5000).Should().BeTrue();
        effect.IsExpired(5001).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNeverExpire_WhenIsPermanent()
    {
        var effect = CombatStatusEffect.Create(
            "destinee-cruelle:dot", "Une destinée cruelle", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 0, magnitude: 10, tickInterval: 2500,
            isMagnitudePercentOfMax: true, isPermanent: true);

        effect.IsExpired(int.MaxValue - 1).Should().BeFalse();
        effect.ConsumeDueTicks(10_000_000, maxVitalityForPercent: 100).Should().BeGreaterThan(0,
            because: "a permanent DoT keeps ticking indefinitely.");
    }

    [Fact]
    public void ExtendDuration_ShouldBeNoOp_WhenEffectIsPermanent()
    {
        var effect = CombatStatusEffect.Create(
            "destinee-cruelle:dot", "Une destinée cruelle", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 0, isPermanent: true);

        effect.ExtendDuration(500_000);

        effect.IsExpired(500_001).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenCurrentTickBeforeExpiry()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.IsExpired(4999).Should().BeFalse();
    }

    [Fact]
    public void Reinforce_ShouldAddStacks_AndNeverChangeDuration()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000, stacks: 1);

        effect.Reinforce(2);

        effect.Stacks.Should().Be(3);
        effect.ExpiresAtTick.Should().Be(5000);
    }

    [Fact]
    public void Reinforce_ShouldClampStacks_ToMaxStacks()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000, stacks: 50);

        effect.Reinforce(100, maxStacks: 99);

        effect.Stacks.Should().Be(99);
    }

    [Fact]
    public void Reinforce_ShouldNotReduceStacks_BelowOne()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000, stacks: 1);

        effect.Reinforce(-10);

        effect.Stacks.Should().Be(1);
    }

    [Fact]
    public void ExtendDuration_ShouldPushExpiryLater_WhenNewValueIsLater()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.ExtendDuration(8000);

        effect.ExpiresAtTick.Should().Be(8000);
    }

    [Fact]
    public void ExtendDuration_ShouldBeNoOp_WhenNewValueIsNotLater()
    {
        var effect = CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, 0, 5000);

        effect.ExtendDuration(3000);

        effect.ExpiresAtTick.Should().Be(5000);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldReturnZero_ForNonPeriodicEffects()
    {
        var effect = CombatStatusEffect.Create("stun", "Stun", StatusEffectKind.Stun, 0, 3000);

        var result = effect.ConsumeDueTicks(5000);

        result.Should().Be(0);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldReturnTotalMagnitude_ForDueTicks()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 6000,
            magnitude: 10, tickInterval: 1400);

        var result = effect.ConsumeDueTicks(4200);

        result.Should().Be(30);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldAdvanceNextTickAtTick()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 6000,
            magnitude: 10, tickInterval: 1400);

        effect.ConsumeDueTicks(2800);

        effect.NextTickAtTick.Should().Be(4200);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldStopAtExpiry()
    {
        var effect = CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 3000,
            magnitude: 10, tickInterval: 1400);

        var result = effect.ConsumeDueTicks(10000);

        result.Should().Be(20);
    }

    [Fact]
    public void IsPeriodic_ShouldBeTrue_ForGuardOverTime()
    {
        var effect = CombatStatusEffect.Create(
            "guard-regen", "Garde continue", StatusEffectKind.GuardOverTime,
            currentTick: 0, durationTicks: 6000,
            magnitude: 8, tickInterval: 1400);

        effect.IsPeriodic.Should().BeTrue();
    }

    [Fact]
    public void ConsumeDueTicks_ShouldReturnTotalMagnitude_ForGuardOverTime()
    {
        var effect = CombatStatusEffect.Create(
            "guard-regen", "Garde continue", StatusEffectKind.GuardOverTime,
            currentTick: 0, durationTicks: 6000,
            magnitude: 8, tickInterval: 1400);

        var result = effect.ConsumeDueTicks(4200);

        result.Should().Be(24);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldComputePercentOfMaxVitality_WhenMagnitudeIsPercentOfMax()
    {
        var effect = CombatStatusEffect.Create(
            "construction-perpetuelle", "Construction perpétuelle", StatusEffectKind.HealOverTime,
            currentTick: 0, durationTicks: 3000,
            magnitude: 10, tickInterval: 1400,
            isMagnitudePercentOfMax: true);

        // One tick due (at 1400), 10% of 200 max vitality = 20.
        var result = effect.ConsumeDueTicks(1400, maxVitalityForPercent: 200);

        result.Should().Be(20);
    }

    [Fact]
    public void ConsumeDueTicks_ShouldUseFlatMagnitude_WhenNotPercentOfMax()
    {
        var effect = CombatStatusEffect.Create(
            "regen", "Régénération", StatusEffectKind.HealOverTime,
            currentTick: 0, durationTicks: 3000,
            magnitude: 10, tickInterval: 1400);

        // maxVitalityForPercent is ignored when the flag isn't set.
        var result = effect.ConsumeDueTicks(1400, maxVitalityForPercent: 200);

        result.Should().Be(10);
    }

    [Fact]
    public void Create_StatModifier_ShouldNotBePeriodic()
    {
        var effect = CombatStatusEffect.Create(
            "atk-down", "Attack Down", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000,
            magnitude: -5, stat: CombatStat.AttackPower);

        effect.IsPeriodic.Should().BeFalse();
    }

    [Fact]
    public void Create_Stun_ShouldNotBePeriodic()
    {
        var effect = CombatStatusEffect.Create(
            "stun", "Stun", StatusEffectKind.Stun,
            currentTick: 0, durationTicks: 3000);

        effect.IsPeriodic.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEffect_WithGivenState()
    {
        var effect = CombatStatusEffect.Rehydrate(
            "poison", "Poison", StatusEffectKind.DamageOverTime, null,
            CombatStat.None, 10, 2, 1400, 2800, 5000);

        effect.Key.Should().Be("poison");
        effect.Stacks.Should().Be(2);
        effect.NextTickAtTick.Should().Be(2800);
        effect.ExpiresAtTick.Should().Be(5000);
    }
}
