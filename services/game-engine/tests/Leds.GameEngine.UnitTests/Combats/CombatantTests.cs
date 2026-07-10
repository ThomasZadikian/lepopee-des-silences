using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantTests
{
    [Fact]
    public void CreateAlly_ShouldSucceed_WithValidData()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.SourceKey.Should().Be("player.self");
        combatant.DisplayName.Should().Be("Hero");
        combatant.Side.Should().Be(CombatantSide.Player);
        combatant.Archetype.Should().Be("Fighter");
        combatant.MaxVitality.Should().Be(100);
        combatant.CurrentVitality.Should().Be(100);
        combatant.Guard.Should().Be(0);
        combatant.Mana.Should().Be(0);
        combatant.Charge.Should().Be(0);
        combatant.Status.Should().Be(CombatantStatus.Active);
        combatant.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void CreateEnemy_ShouldSucceed_WithValidData()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.SourceKey.Should().Be("enemy.sentinel");
        combatant.DisplayName.Should().Be("Sentinel");
        combatant.Side.Should().Be(CombatantSide.Enemy);
        combatant.Archetype.Should().Be("Guard");
        combatant.MaxVitality.Should().Be(80);
        combatant.CurrentVitality.Should().Be(80);
        combatant.Status.Should().Be(CombatantStatus.Active);
    }

    [Fact]
    public void GainTempoMomentum_ShouldAccumulate_UpToTheCap()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.GainTempoMomentum(300);
        combatant.GainTempoMomentum(300);

        combatant.TempoMomentumPerMille.Should().Be(500, because: "momentum is capped at 500 per-mille.");
    }

    [Fact]
    public void DecayTempoMomentum_ShouldReduceOverElapsedTicks()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.GainTempoMomentum(100);

        combatant.DecayTempoMomentum(50); // 50 ticks / 10 ticks-per-point = 5 points lost.

        combatant.TempoMomentumPerMille.Should().Be(95);
    }

    [Fact]
    public void DecayTempoMomentum_ShouldNeverGoBelowZero()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.GainTempoMomentum(20);

        combatant.DecayTempoMomentum(1000);

        combatant.TempoMomentumPerMille.Should().Be(0);
    }

    [Fact]
    public void RegisterAtbAction_ShouldResetTempoMomentum_ToZero()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.GainTempoMomentum(200);

        combatant.RegisterAtbAction(currentTick: 0, recoveryTicks: 10);

        combatant.TempoMomentumPerMille.Should().Be(0);
    }

    [Fact]
    public void RecalculateAtbFillPerTick_ShouldReactToLiveEffectiveSpeed()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, speed: 10);

        combatant.RecalculateAtbFillPerTick(opponentAverageEffectiveSpeed: 10);
        var before = combatant.AtbFillPerTick;

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "speed-buff", "Speed Buff", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 6000, magnitude: 10, stat: Leds.GameEngine.Domain.Combats.StatusEffects.CombatStat.Speed));

        combatant.RecalculateAtbFillPerTick(opponentAverageEffectiveSpeed: 10);

        combatant.AtbFillPerTick.Should().BeGreaterThan(before,
            because: "tempo must react live to a mid-combat Speed buff, not stay frozen at the pre-buff value.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSourceKeyIsEmpty()
    {
        var act = () => Combatant.Create(CombatantId.New(), "", "Hero", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant source key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDisplayNameIsEmpty()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant display name is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxVitalityIsZeroOrNegative()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 0, 0, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant max vitality must be greater than zero.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCurrentVitalityExceedsMaxVitality()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 100, 150, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant current vitality must be between zero and max vitality.");
    }

    [Fact]
    public void MarkDefeated_ShouldSetStatusDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.MarkDefeated();

        combatant.Status.Should().Be(CombatantStatus.Defeated);
        combatant.IsDefeated.Should().BeTrue();
        combatant.CurrentVitality.Should().Be(0);
    }

    [Fact]
    public void MarkDefeated_ShouldThrow_WhenAlreadyDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.MarkDefeated();

        var act = () => combatant.MarkDefeated();

        act.Should().Throw<DomainException>().WithMessage("Combatant is already defeated.");
    }

    [Fact]
    public void ApplyDamage_ShouldReduceVitality()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(12);

        combatant.CurrentVitality.Should().Be(68);
        combatant.Guard.Should().Be(0);
        combatant.Status.Should().Be(CombatantStatus.Active);
    }

    [Fact]
    public void ApplyDamage_ShouldConsumeGuardBeforeVitality()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        combatant.GainGuard(5);

        combatant.ApplyDamage(12);

        combatant.Guard.Should().Be(0);
        combatant.CurrentVitality.Should().Be(73);
    }

    [Fact]
    public void ApplyDamage_ShouldNotReduceVitalityBelowZero()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(120);

        combatant.CurrentVitality.Should().Be(0);
    }

    [Fact]
    public void ApplyDamage_ShouldMarkCombatantDefeated_WhenVitalityReachesZero()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(80);

        combatant.Status.Should().Be(CombatantStatus.Defeated);
        combatant.IsDefeated.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyDamage_ShouldThrow_WhenAmountIsZeroOrNegative(int amount)
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        var act = () => combatant.ApplyDamage(amount);

        act.Should().Throw<DomainException>().WithMessage("Damage amount must be greater than zero.");
    }

    [Fact]
    public void ApplyDamage_ShouldThrow_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        combatant.MarkDefeated();

        var act = () => combatant.ApplyDamage(1);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatants cannot receive damage.");
    }

    [Fact]
    public void GainGuard_ShouldIncreaseGuard()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.GainGuard(7);

        combatant.Guard.Should().Be(7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GainGuard_ShouldThrow_WhenAmountIsZeroOrNegative(int amount)
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        var act = () => combatant.GainGuard(amount);

        act.Should().Throw<DomainException>().WithMessage("Guard amount must be greater than zero.");
    }

    [Fact]
    public void GainGuard_ShouldThrow_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.MarkDefeated();

        var act = () => combatant.GainGuard(1);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatants cannot gain guard.");
    }

    // -----------------------------------------------------------------------
    // BaseGuard / ResetGuardToBase
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateAlly_WithBaseGuard_ShouldSetBothGuardAndBaseGuard()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);

        combatant.Guard.Should().Be(8);
        combatant.BaseGuard.Should().Be(8);
    }

    [Fact]
    public void ResetGuardToBase_ShouldRestoreGuard_AfterDamageConsumedIt()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.ApplyDamage(5); // guard 8 → 3

        combatant.ResetGuardToBase();

        combatant.Guard.Should().Be(8,
            because: "Guard must be restored to BaseGuard at round start.");
    }

    [Fact]
    public void ResetGuardToBase_ShouldNotReduceGuard_WhenGainGuardExceedsBase()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.GainGuard(5); // guard = 13 > BaseGuard 8

        combatant.ResetGuardToBase();

        combatant.Guard.Should().Be(13,
            because: "ResetGuardToBase must not reduce guard earned via skill.basic.guard.");
    }

    [Fact]
    public void ResetGuardToBase_ShouldDoNothing_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.MarkDefeated();

        var act = () => combatant.ResetGuardToBase();

        act.Should().NotThrow();
        combatant.Guard.Should().Be(8,
            because: "MarkDefeated does not reset guard; baseGuard value persists.");
    }

    [Fact]
    public void EffectiveMagicDamageBonusPercent_ShouldEqualEquipmentValue_WhenNoStatusEffectIsActive()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 10, magicDamageReductionPercent: 5);

        combatant.MagicDamageBonusPercent.Should().Be(10);
        combatant.MagicDamageReductionPercent.Should().Be(5);
        combatant.EffectiveMagicDamageBonusPercent.Should().Be(10);
        combatant.EffectiveMagicDamageReductionPercent.Should().Be(5);
    }

    [Fact]
    public void EffectiveMagicDamageBonusPercent_ShouldSumEquipmentAndStatModifierStatus()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 10, magicDamageReductionPercent: 5);

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "connaissance-academique:bonus", "Connaissance académique", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 12500, magnitude: 10, stat: Leds.GameEngine.Domain.Combats.StatusEffects.CombatStat.MagicDamageBonus));
        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "connaissance-academique:reduction", "Connaissance académique", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 12500, magnitude: 5, stat: Leds.GameEngine.Domain.Combats.StatusEffects.CombatStat.MagicDamageReduction));

        combatant.EffectiveMagicDamageBonusPercent.Should().Be(20,
            because: "equipment (10) + skill-driven StatModifier buff (10) must both contribute.");
        combatant.EffectiveMagicDamageReductionPercent.Should().Be(10,
            because: "equipment (5) + skill-driven StatModifier buff (5) must both contribute.");
    }
}