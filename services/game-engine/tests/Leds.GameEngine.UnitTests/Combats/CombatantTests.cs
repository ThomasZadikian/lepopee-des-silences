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
    public void HasActedThisCombat_ShouldBeFalse_OnANewlyCreatedCombatant()
    {
        Combatant.CreateAlly("player.self", "Hero", "Fighter", 100).HasActedThisCombat.Should().BeFalse();
    }

    [Fact]
    public void MarkActedThisCombat_ShouldSetHasActedThisCombat_ToTrue()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.MarkActedThisCombat();

        combatant.HasActedThisCombat.Should().BeTrue();
    }

    [Fact]
    public void IsActivationBlocked_ShouldBeTrue_WhenSilenced()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.IsActivationBlocked.Should().BeFalse();

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "silence", "Silence", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.Silence,
            currentTick: 0, durationTicks: 2500));

        combatant.IsSilenced.Should().BeTrue();
        combatant.IsActivationBlocked.Should().BeTrue(because: "Silence blocks the next action exactly like Stun.");
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
    public void GainMana_ShouldBeUncapped_WhenCreatedWithoutMaxMana()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.GainMana(1_000_000);

        combatant.Mana.Should().Be(1_000_000);
    }

    [Fact]
    public void GainMana_ShouldClampToMaxMana_WhenSpecifiedAtCreation()
    {
        var combatant = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0,
            mana: 15, charge: 0, maxMana: 20);

        combatant.MaxMana.Should().Be(20);

        combatant.GainMana(10);

        combatant.Mana.Should().Be(20);
    }

    [Fact]
    public void Create_ShouldThrow_WhenManaExceedsMaxMana()
    {
        var act = () => Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0,
            mana: 30, charge: 0, maxMana: 20);

        act.Should().Throw<DomainException>().WithMessage("Combatant mana cannot exceed max mana.");
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
    public void EffectiveHealingBonusPercent_ShouldEqualEquipmentValue_WhenNoStatusEffectIsActive()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            healingBonusPercent: 15);

        combatant.HealingBonusPercent.Should().Be(15);
        combatant.EffectiveHealingBonusPercent.Should().Be(15);
    }

    [Fact]
    public void EffectiveCriticalChanceBonusPercent_ShouldEqualEquipmentValue_WhenNoStatusEffectIsActive()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            criticalChanceBonusPercent: 5);

        combatant.CriticalChanceBonusPercent.Should().Be(5);
        combatant.EffectiveCriticalChanceBonusPercent.Should().Be(5);
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

    [Fact]
    public void EffectiveMagicAttackAndDefense_ShouldDefaultToZero_WhenNotAuthored()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);

        combatant.EffectiveMagicAttack.Should().Be(0);
        combatant.EffectiveMagicDefense.Should().Be(0,
            because: "every combatant predating the Bestiaire chantier must keep a neutral Magic damage ratio (see CombatSkillEffectResolver.StatModifierDamageMultiplier).");
    }

    [Fact]
    public void EffectiveMagicAttackAndDefense_ShouldEqualAuthoredBaseValue()
    {
        var combatant = Combatant.CreateEnemy("enemy.eclat-eveille", "Éclat Éveillé", "Guard", 44, magicAttack: 15, magicDefense: 12);

        combatant.EffectiveMagicAttack.Should().Be(15);
        combatant.EffectiveMagicDefense.Should().Be(12);
    }

    [Fact]
    public void EffectiveMagicAttack_ShouldSumBaseValueAndStatModifierStatus()
    {
        var combatant = Combatant.CreateEnemy("enemy.eclat-eveille", "Éclat Éveillé", "Guard", 44, magicAttack: 15);

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "facette:bonus", "Facette", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: 5, stat: Leds.GameEngine.Domain.Combats.StatusEffects.CombatStat.MagicAttack));

        combatant.EffectiveMagicAttack.Should().Be(20,
            because: "authored base (15) + skill-driven StatModifier buff (5) must both contribute, mirroring EffectiveAttackPower.");
    }

    [Fact]
    public void EffectiveDotDamageBonusPercent_ShouldSumEquipmentAndStatModifierStatus()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 0, magicDamageReductionPercent: 0, criticalChanceBonusPercent: 0,
            dotDamageBonusPercent: 5);

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "ecriture-continuelle:bonus", "Écriture continuelle", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 12500, magnitude: 10, stat: Leds.GameEngine.Domain.Combats.StatusEffects.CombatStat.DotDamageBonus));

        combatant.DotDamageBonusPercent.Should().Be(5);
        combatant.EffectiveDotDamageBonusPercent.Should().Be(15,
            because: "equipment (5) + skill-driven StatModifier buff (10) must both contribute.");
    }

    [Fact]
    public void ApplyStatusEffect_ShouldAddStacks_ButNeverChangeRemainingDuration_WhenReapplyingTheSameKey()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "poison", "Poison", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 7000, magnitude: 5, stacks: 2, tickInterval: 1400));

        // Re-cast the same DoT (fresh full duration from "now") while 2 stacks / 7000
        // ticks remain — must add a stack WITHOUT touching remaining duration.
        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "poison", "Poison", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 20000, magnitude: 5, stacks: 1, tickInterval: 1400));

        var effect = combatant.StatusEffects.Should().ContainSingle(e => e.Key == "poison").Subject;
        effect.Stacks.Should().Be(3);
        effect.ExpiresAtTick.Should().Be(7000,
            because: "stacking must never refresh/extend remaining duration — only an explicit extend-duration mechanic does.");
    }

    [Fact]
    public void Skills_ShouldIncludeGrantedSkills_WhileASkillGrantStatusEffectIsActive()
    {
        var borrowed = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 22);
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.PermanentSkills.Should().NotContain(s => s.Key == "canon.skill.flamme-froide");

        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "creation:target", "Création", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.SkillGrant,
            currentTick: 0, durationTicks: 25000, grantedSkills: [borrowed]));

        combatant.Skills.Should().Contain(s => s.Key == "canon.skill.flamme-froide");
        combatant.PermanentSkills.Should().NotContain(s => s.Key == "canon.skill.flamme-froide",
            because: "a temporarily granted skill must never be treated as permanently owned.");
    }

    [Fact]
    public void Skills_ShouldStopIncludingGrantedSkills_AfterTheGrantExpires()
    {
        var borrowed = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 22);
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.ApplyStatusEffect(Leds.GameEngine.Domain.Combats.StatusEffects.CombatStatusEffect.Create(
            "creation:target", "Création", Leds.GameEngine.Domain.Combats.StatusEffects.StatusEffectKind.SkillGrant,
            currentTick: 0, durationTicks: 25000, grantedSkills: [borrowed]));

        combatant.TickStatusEffects(currentTick: 25000);

        combatant.Skills.Should().NotContain(s => s.Key == "canon.skill.flamme-froide");
    }

}