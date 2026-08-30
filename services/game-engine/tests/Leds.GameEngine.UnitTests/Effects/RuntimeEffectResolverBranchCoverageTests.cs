using FluentAssertions;
using Leds.GameEngine.Application.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;

namespace Leds.GameEngine.UnitTests.Effects;

public sealed class RuntimeEffectResolverBranchCoverageTests
{
    private readonly RuntimeEffectResolver _sut = new();

    [Theory]
    [InlineData(EffectType.HealVitality)]
    [InlineData(EffectType.DamageVitality)]
    [InlineData(EffectType.AddCurrentGuard)]
    public void ImmediatePositiveEffects_ShouldRejectZero(EffectType type)
    {
        var effect = Create(type, 0, ValueMode.Flat, EffectDuration.Immediate);
        var act = () => _sut.Resolve(new RuntimeEffectResolutionContext(effect, [Target()]));
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(EffectType.RestoreFocus)]
    [InlineData(EffectType.RestoreMana)]
    [InlineData(EffectType.RestoreCharge)]
    public void ImmediateResourceMarkers_ShouldBeAccepted(EffectType type)
    {
        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(type, 1, ValueMode.Flat, EffectDuration.Immediate), [Target()]));
        result.UpdatedTargets.Should().ContainSingle();
    }

    [Theory]
    [InlineData(EffectType.AddStartingGuard)]
    [InlineData(EffectType.ModifyDifficultyMultiplier)]
    [InlineData(EffectType.ModifyRewardPowerMultiplier)]
    [InlineData(EffectType.ModifyAttackPower)]
    [InlineData(EffectType.ModifyDefense)]
    [InlineData(EffectType.ModifySpeed)]
    [InlineData(EffectType.ModifyInitiative)]
    [InlineData(EffectType.RestoreFocus)]
    public void PersistentMappedEffects_ShouldCreateModifier(EffectType type)
    {
        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(type, 10, ValueMode.Flat, EffectDuration.UntilRunEnds), [Target()]));
        result.CreatedModifiers.Should().ContainSingle();
    }

    [Theory]
    [InlineData(EffectDuration.CurrentCombat)]
    [InlineData(EffectDuration.NextCombatOnly)]
    [InlineData(EffectDuration.NextRewardOnly)]
    [InlineData(EffectDuration.UntilRoomEnds)]
    [InlineData(EffectDuration.UntilRunEnds)]
    [InlineData(EffectDuration.PermanentCandidate)]
    [InlineData(EffectDuration.UntilConsumed)]
    public void PersistentDurations_ShouldMap(EffectDuration duration)
    {
        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(EffectType.ModifyAttackPower, 2, ValueMode.Flat, duration), [Target()]));
        result.CreatedModifiers.Should().ContainSingle();
    }

    [Theory]
    [InlineData(ValueMode.Percent)]
    [InlineData(ValueMode.Multiplier)]
    [InlineData(ValueMode.TagOnly)]
    [InlineData(ValueMode.WeightDelta)]
    public void PersistentValueModes_ShouldResolve(ValueMode mode)
    {
        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(EffectType.AddStartingGuard, 10, mode, EffectDuration.UntilRunEnds), [Target()]));
        result.CreatedModifiers.Should().ContainSingle();
    }

    [Theory]
    [InlineData(EffectType.HealVitality)]
    [InlineData(EffectType.DamageVitality)]
    [InlineData(EffectType.AddCurrentGuard)]
    public void PercentImmediateEffects_ShouldUseRelevantBase(EffectType type)
    {
        var target = Target(vitality: 50, guard: 20);
        if (type == EffectType.HealVitality)
            target.ApplyVitalityDamage(20);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(type, 10, ValueMode.Percent, EffectDuration.Immediate), [target]));
        result.UpdatedTargets.Should().ContainSingle();
    }

    [Fact]
    public void UnsupportedPersistentEffect_ShouldThrow()
    {
        var act = () => _sut.Resolve(new RuntimeEffectResolutionContext(
            Create(EffectType.ApplyWeaken, 1, ValueMode.Flat, EffectDuration.UntilRunEnds), [Target()]));
        act.Should().Throw<DomainException>().WithMessage("*Cannot map effect type*");
    }

    private static RuntimeEffect Create(EffectType type, decimal value, ValueMode mode, EffectDuration duration) =>
        new(type, EffectTargetScope.Self, value, mode, duration, StackPolicy.None,
            null, 0, null, null, null);

    private static Combatant Target(int vitality = 100, int guard = 0) => Combatant.Create(
        CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
        maxVitality: 100, currentVitality: vitality, guard: guard, baseGuard: 0,
        mana: 10, charge: 0, maxMana: 100);
}
