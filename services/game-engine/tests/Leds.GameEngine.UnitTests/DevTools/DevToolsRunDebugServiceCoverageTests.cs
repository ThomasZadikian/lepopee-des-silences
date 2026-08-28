using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.DevTools;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.DevTools;

public sealed class DevToolsRunDebugServiceCoverageTests
{
    private static readonly MethodInfo BuildDebugStatusMethod =
        typeof(DevToolsRunDebugService).GetMethod(
            "BuildDebugStatus",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("BuildDebugStatus was not found.");

    private static readonly MethodInfo MapClimateMethod =
        typeof(DevToolsRunDebugService).GetMethod(
            "MapClimate",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("MapClimate was not found.");

    [Theory]
    [InlineData("poison", StatusEffectKind.DamageOverTime, 8, null)]
    [InlineData("burn", StatusEffectKind.DamageOverTime, 12, null)]
    [InlineData("regen", StatusEffectKind.HealOverTime, 10, null)]
    [InlineData("atk-up", StatusEffectKind.StatModifier, 8, "AttackPower")]
    [InlineData("atk-down", StatusEffectKind.StatModifier, -8, "AttackPower")]
    [InlineData("def-up", StatusEffectKind.StatModifier, 8, "Defense")]
    [InlineData("def-down", StatusEffectKind.StatModifier, -8, "Defense")]
    [InlineData("stun", StatusEffectKind.Stun, 0, null)]
    [InlineData("silence", StatusEffectKind.Silence, 0, null)]
    [InlineData("slow", StatusEffectKind.StatModifier, -50, "Speed")]
    public void BuildDebugStatus_ShouldMapEverySupportedPreset(
        string key,
        StatusEffectKind expectedKind,
        int expectedMagnitude,
        string? expectedStat)
    {
        var effect = InvokeBuildStatus($"  {key.ToUpperInvariant()}  ", stacks: 3, durationTicks: 2, currentTick: 50);

        effect.Kind.Should().Be(expectedKind);
        effect.Magnitude.Should().Be(expectedMagnitude);
        effect.Stacks.Should().Be(3);
        effect.ExpiresAtTick.Should().Be(2050);
        if (expectedStat is not null)
        {
            effect.Stat.ToString().Should().Be(expectedStat);
        }
    }

    [Fact]
    public void BuildDebugStatus_ShouldUseDefaultDurationAndClampStacks()
    {
        var effect = InvokeBuildStatus("poison", stacks: 0, durationTicks: 0, currentTick: 10);

        effect.Stacks.Should().Be(1);
        effect.ExpiresAtTick.Should().Be(6010);
    }

    [Fact]
    public void BuildDebugStatus_ShouldRejectUnknownPreset()
    {
        var action = () => InvokeBuildStatus("unknown-status", 1, 1, 0);

        action.Should().Throw<DomainException>()
            .WithMessage("*Unknown debug status*");
    }

    [Theory]
    [InlineData("none", null)]
    [InlineData(" NONE ", null)]
    [InlineData("grey", 1d)]
    [InlineData("rain", 2d)]
    [InlineData("heatwave", 3d)]
    [InlineData("hail", 4d)]
    public void MapClimate_ShouldMapEverySupportedClimate(string climate, double? expected)
    {
        InvokeMapClimate(climate).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("snow")]
    public void MapClimate_ShouldRejectUnsupportedClimate(string? climate)
    {
        var action = () => InvokeMapClimate(climate);

        action.Should().Throw<DomainException>()
            .WithMessage("*Unsupported room climate*");
    }

    private static CombatStatusEffect InvokeBuildStatus(
        string statusKey,
        int stacks,
        int durationTicks,
        int currentTick)
    {
        try
        {
            return (CombatStatusEffect)BuildDebugStatusMethod.Invoke(
                null,
                [statusKey, stacks, durationTicks, currentTick])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static double? InvokeMapClimate(string? climate)
    {
        try
        {
            return (double?)MapClimateMethod.Invoke(null, [climate]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
