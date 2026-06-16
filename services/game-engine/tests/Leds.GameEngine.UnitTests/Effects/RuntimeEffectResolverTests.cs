using FluentAssertions;
using Leds.GameEngine.Application.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Effects;

public sealed class RuntimeEffectResolverTests
{
    private readonly RuntimeEffectResolver _sut = new();

    private static Combatant CreateTestCombatant(
        int maxVitality = 100,
        int currentVitality = 100,
        int guard = 0,
        int baseGuard = 0)
    {
        return Combatant.Create(
            CombatantId.New(),
            "test.hero",
            "Hero",
            CombatantSide.Player,
            "Fighter",
            maxVitality,
            currentVitality,
            guard,
            baseGuard,
            mana: 0,
            charge: 0);
    }

    // -----------------------------------------------------------------------
    // HealVitality
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldApplyHealVitality()
    {
        var target = CreateTestCombatant(currentVitality: 50);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, 30m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.UpdatedTargets.Should().ContainSingle();
        result.CreatedModifiers.Should().BeEmpty();
        target.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldNotHealAboveMaxVitality()
    {
        var target = CreateTestCombatant(currentVitality: 90);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, 30m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(100);
    }

    // -----------------------------------------------------------------------
    // DamageVitality
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldApplyDamageVitality()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.DamageVitality, EffectTargetScope.SingleEnemy, 25m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(75);
    }

    [Fact]
    public void Resolve_ShouldNotReduceVitalityBelowZero()
    {
        var target = CreateTestCombatant(currentVitality: 10);
        var effect = new RuntimeEffect(
            EffectType.DamageVitality, EffectTargetScope.SingleEnemy, 50m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(0);
        target.IsDefeated.Should().BeTrue();
    }

    [Fact]
    public void Resolve_GuardShouldAbsorbDamageBeforeVitality()
    {
        var target = CreateTestCombatant(guard: 20);
        var effect = new RuntimeEffect(
            EffectType.DamageVitality, EffectTargetScope.SingleEnemy, 15m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(100);
        target.Guard.Should().Be(5);
    }

    // -----------------------------------------------------------------------
    // AddCurrentGuard
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldAddCurrentGuard()
    {
        var target = CreateTestCombatant(guard: 10);
        var effect = new RuntimeEffect(
            EffectType.AddCurrentGuard, EffectTargetScope.Self, 15m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.Guard.Should().Be(25);
    }

    // -----------------------------------------------------------------------
    // AddStartingGuard (persistent)
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldCreateRunModifier_ForAddStartingGuard()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(
            effect, [target], SourceType: "RunItem", SourceKey: "item.consumable.eclat-de-garde"));

        result.UpdatedTargets.Should().BeEmpty();
        result.CreatedModifiers.Should().ContainSingle();
        var modifier = result.CreatedModifiers.Single();
        modifier.Type.Should().Be(RunModifierType.StartingGuardBonus);
        modifier.Value.Should().Be(8);
        modifier.Duration.Should().Be(RunModifierDuration.UntilRunEnds);
        modifier.SourceType.Should().Be("RunItem");
        modifier.SourceKey.Should().Be("item.consumable.eclat-de-garde");
        modifier.ValueMode.Should().Be("Flat");
        modifier.StackPolicy.Should().Be("Additive");
    }

    // -----------------------------------------------------------------------
    // ValueMode tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldApplyFlatValueMode()
    {
        var target = CreateTestCombatant(currentVitality: 50);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, 25m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(75);
    }

    [Fact]
    public void Resolve_ShouldApplyPercentValueMode()
    {
        var target = CreateTestCombatant(currentVitality: 50);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, 30m, ValueMode.Percent,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldApplyMultiplierValueMode()
    {
        var target = CreateTestCombatant(currentVitality: 50);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, 0.3m, ValueMode.Multiplier,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        target.CurrentVitality.Should().Be(80);
    }

    // -----------------------------------------------------------------------
    // StackPolicy on persistent effects
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldCreateModifier_WithAdditiveStackPolicy()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().StackPolicy.Should().Be("Additive");
    }

    [Fact]
    public void Resolve_ShouldCreateModifier_WithNoneStackPolicy()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.None,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().StackPolicy.Should().Be("None");
    }

    [Fact]
    public void Resolve_ShouldCreateModifier_WithReplaceStackPolicy()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.Replace,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().StackPolicy.Should().Be("Replace");
    }

    [Fact]
    public void Resolve_ShouldCreateModifier_WithHighestOnlyStackPolicy()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.HighestOnly,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().StackPolicy.Should().Be("HighestOnly");
    }

    // -----------------------------------------------------------------------
    // Persistent effect duration mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_UntilRunEndsModifier_ShouldHaveCorrectDuration()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.Run, 8m, ValueMode.Flat,
            EffectDuration.UntilRunEnds, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().Duration.Should().Be(RunModifierDuration.UntilRunEnds);
    }

    [Fact]
    public void Resolve_NextCombatOnlyModifier_ShouldHaveCorrectDuration()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.AddStartingGuard, EffectTargetScope.NextCombat, 8m, ValueMode.Flat,
            EffectDuration.NextCombatOnly, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        result.CreatedModifiers.Single().Duration.Should().Be(RunModifierDuration.NextCombatOnly);
    }

    // -----------------------------------------------------------------------
    // Error cases
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldThrow_ForNegativeHealValue()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.Self, -10m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        var act = () => _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        act.Should().Throw<DomainException>()
            .WithMessage("HealVitality requires a positive value.");
    }

    [Fact]
    public void Resolve_ShouldThrow_ForUnsupportedImmediateEffectType()
    {
        var target = CreateTestCombatant();
        var effect = new RuntimeEffect(
            EffectType.ModifyDifficultyMultiplier, EffectTargetScope.Run, 0.1m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        var act = () => _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target]));

        act.Should().Throw<DomainException>()
            .WithMessage("*ModifyDifficultyMultiplier*");
    }

    // -----------------------------------------------------------------------
    // Multiple targets
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldApplyEffect_ToMultipleTargets()
    {
        var target1 = CreateTestCombatant(currentVitality: 50);
        var target2 = CreateTestCombatant(currentVitality: 60);
        var effect = new RuntimeEffect(
            EffectType.HealVitality, EffectTargetScope.AllAllies, 20m, ValueMode.Flat,
            EffectDuration.Immediate, StackPolicy.Additive,
            null, 0, null, null, null);

        var result = _sut.Resolve(new RuntimeEffectResolutionContext(effect, [target1, target2]));

        result.UpdatedTargets.Should().HaveCount(2);
        target1.CurrentVitality.Should().Be(70);
        target2.CurrentVitality.Should().Be(80);
    }
}
