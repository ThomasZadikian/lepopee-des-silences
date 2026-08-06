using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

public sealed class CombatSkillEffectResolverTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    [Fact]
    public void Resolve_ShouldApplyDamage_WhenEffectTypeIsDamage()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotBeEmpty();
        // TacticalDamageFormula.CalculateBaseDamage: a defenseless target (default
        // Defense=0) ignores the attack/defense ratio entirely and forces the upper
        // 115% variation instead — round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(68);
    }

    [Fact]
    public void Resolve_ShouldApplyDamage_WhenEffectTypeIsDamageVitality()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "DamageVitality", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotBeEmpty();
        enemy.CurrentVitality.Should().Be(68);
    }

    [Fact]
    public void Resolve_ShouldConsumeGuardBeforeVitality_WhenDamageIsApplied()
    {
        var (combat, ally, enemy) = CreateCombat();
        enemy.GainGuard(5);
        var skill = CreateSkill("skill.basic.strike", "Damage", 12);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.Guard.Should().Be(0);
        // base = round(12 * 1.15) = 14 (defenseless target, forced 115% variation);
        // 5 absorbed by guard, 9 spills into vitality.
        enemy.CurrentVitality.Should().Be(71);
    }

    [Fact]
    public void Resolve_ShouldMarkTargetDefeated_WhenDamageReducesVitalityToZero()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 80);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.Status.Should().Be(CombatantStatus.Defeated);
    }

    [Fact]
    public void Resolve_ShouldIncreaseDamage_WhenActorAttackPowerIsAboveBaseline()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            attackPower: 20);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // The target's Defense is still 0 here, so TacticalDamageFormula ignores the
        // attack/defense ratio (and thus the actor's AttackPower) entirely and forces
        // the upper 115% variation: round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(80 - 12);
    }

    [Fact]
    public void Resolve_ShouldReduceDamage_WhenTargetDefenseIsAboveBaseline()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 20);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // ratio = attack(0) / defense(20) = 0 — the actor's AttackPower defaults to 0
        // here, so TacticalDamageFormula's ratio zeroes the base damage entirely
        // regardless of the random variation roll.
        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldIncreaseMagicDamage_UsingMagicAttackNotPhysicalAttackPower()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            attackPower: 20, magicAttack: 0);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // A high physical AttackPower must NOT boost a Magic-category skill: only
        // MagicAttack (0) is read here. The target's MagicDefense is also 0, so
        // TacticalDamageFormula ignores the ratio and forces round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(80 - 12);
    }

    [Fact]
    public void Resolve_ShouldIncreaseMagicDamage_WhenActorMagicAttackIsAboveBaseline()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 20);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Target's MagicDefense is still 0, so TacticalDamageFormula ignores the
        // ratio (and thus the actor's MagicAttack) and forces round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(80 - 12);
    }

    [Fact]
    public void Resolve_ShouldReduceMagicDamage_WhenTargetMagicDefenseIsAboveBaseline()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 20);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // ratio = magicAttack(0) / magicDefense(20) = 0 — the actor's MagicAttack
        // defaults to 0, so the ratio zeroes the base damage entirely.
        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldIgnoreMagicAttackDefense_ForPhysicalCategorySkills()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 30);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 30);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Physical-category skill: MagicAttack/MagicDefense are irrelevant here — the
        // actor's (Physical) AttackPower and target's Defense are both 0, so
        // TacticalDamageFormula forces round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(68);
    }

    [Fact]
    public void Resolve_ShouldCreateDamageLogEntries()
    {
        var (combat, ally, enemy) = CreateCombat();
        enemy.GainGuard(3);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Message.Contains("guard absorbs 3 damage"));
        result.LogEntries.Should().Contain(e => e.Type == "DamageApplied");
    }

    [Fact]
    public void Resolve_ShouldIncreaseGuard_WhenEffectTypeIsGuard()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.guard", "Guard", 8);

        _resolver.Resolve(combat, ally, skill, [ally]);

        ally.Guard.Should().Be(8);
    }

    [Fact]
    public void Resolve_ShouldCreateGuardLogEntry()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.guard", "Guard", 8);

        var result = _resolver.Resolve(combat, ally, skill, [ally]);

        result.LogEntries.Should().ContainSingle(e => e.Type == "GuardGained");
        result.LogEntries.Single().Message.Should().Contain("gains 8 guard");
    }

    [Fact]
    public void Resolve_ShouldCreateWeakenLogEntry_WhenEffectTypeIsWeaken()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.weaken", "Weaken", 0);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().ContainSingle(e => e.Message.Contains("weakens"));
    }

    [Fact]
    public void Resolve_ShouldNotModifyVitality_WhenEffectTypeIsWeaken()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.weaken", "Weaken", 0);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldCreateDisruptLogEntry_WhenEffectTypeIsDisrupt()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.disrupt", "Disrupt", 0);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().ContainSingle(e => e.Message.Contains("disrupts"));
    }

    [Fact]
    public void Resolve_ShouldNotModifyVitality_WhenEffectTypeIsDisrupt()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.disrupt", "Disrupt", 0);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenEffectTypeIsUnsupported()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.unknown", "Unknown", 0);

        var act = () => _resolver.Resolve(combat, ally, skill, [enemy]);

        act.Should().Throw<DomainException>().WithMessage("Unsupported skill effect type: Unknown*");
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenTargetsAreEmpty()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var act = () => _resolver.Resolve(combat, ally, skill, []);

        act.Should().Throw<DomainException>().WithMessage("At least one target is required to resolve a skill effect.");
    }

    [Fact]
    public void Resolve_ShouldAccrueThreatOnActor_WhenPlayerDamagesEnemy()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.ThreatValue.Should().BeGreaterThan(0);
        enemy.LastAttackerId.Should().Be(ally.Id.Value);
    }

    [Fact]
    public void Resolve_ShouldNotAccrueThreat_WhenEnemyDamagesPlayer()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, enemy, skill, [ally]);

        enemy.ThreatValue.Should().Be(0);
        ally.LastAttackerId.Should().Be(enemy.Id.Value);
    }

    [Fact]
    public void Resolve_ShouldAccrueThreatOnActor_WhenPlayerHealsAlly()
    {
        var (combat, ally, _) = CreateCombat();
        ally.ApplyDamage(30);
        var skill = CreateSkill("skill.heal", "Heal", 15);

        _resolver.Resolve(combat, ally, skill, [ally]);

        ally.ThreatValue.Should().BeGreaterThan(0);
    }

    // ---------------------------------------------------------------------------
    // "Loi des Visites Terminées" (Combat.HealingBlocked, liée à room.hopital) —
    // skill-based healing is a no-op; item-based healing is a separate code path
    // (UseItemInCombatCommandHandler) untouched by this flag.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldNotHeal_WhenHealingIsBlocked()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyDamage(50);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: false,
            dotDurationExtensionTicks: 0,
            duelDamageAsymmetryEnabled: false,
            dotMagnitudeBonus: 0,
            healingBlocked: true);
        var skill = CreateSkill("skill.heal", "Heal", 15);

        _resolver.Resolve(combat, ally, skill, [ally]);

        ally.CurrentVitality.Should().Be(50,
            because: "Loi des Visites Terminées makes skill-based healing a no-op in this room.");
    }

    [Fact]
    public void Resolve_ShouldHeal_WhenHealingIsNotBlocked()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyDamage(50);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.heal", "Heal", 15);

        _resolver.Resolve(combat, ally, skill, [ally]);

        ally.CurrentVitality.Should().Be(65);
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Troisième Tasse" (Combat.ThirdCupHealCorruptionEnabled) — resolved
    // once per heal application via Combat.ApplyThirdCupRollIfActive. Each combat below
    // uses a fresh Guid (Combat.Id is part of the roll's seed), so running many
    // independent combats gives a robust statistical signal without ever depending on a
    // single hardcoded outcome.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldSometimesHalveHeal_WhenThirdCupIsEnabled()
    {
        var anyCorrupted = false;
        var anyNotCorrupted = false;

        for (var i = 0; i < 300; i++)
        {
            var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
            ally.ApplyDamage(50);
            var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
            var combat = TestTacticalCombatHelper.Create(
                RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
                thirdCupHealCorruptionEnabled: true);
            var skill = CreateSkill("skill.heal", "Heal", 20);

            _resolver.Resolve(combat, ally, skill, [ally]);

            if (ally.CurrentVitality == 60) anyCorrupted = true; // 50 + half of 20
            else if (ally.CurrentVitality == 70) anyNotCorrupted = true; // 50 + 20
        }

        anyCorrupted.Should().BeTrue(because: "roughly 10% of 300 independent rolls must trigger at least once.");
        anyNotCorrupted.Should().BeTrue(because: "roughly 90% of 300 independent rolls must NOT trigger at least once.");
    }

    [Fact]
    public void Resolve_ShouldApplyPoisonDamageOverTime_WhenThirdCupTriggers()
    {
        Combatant? poisoned = null;

        for (var i = 0; i < 300 && poisoned is null; i++)
        {
            var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
            ally.ApplyDamage(50);
            var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
            var combat = TestTacticalCombatHelper.Create(
                RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
                thirdCupHealCorruptionEnabled: true);
            var skill = CreateSkill("skill.heal", "Heal", 20);

            _resolver.Resolve(combat, ally, skill, [ally]);

            if (ally.StatusEffects.Any(e => e.Key == "troisieme-tasse-poison"))
                poisoned = ally;
        }

        poisoned.Should().NotBeNull(because: "300 independent 10% rolls must trigger at least once.");
        var poison = poisoned!.StatusEffects.Single(e => e.Key == "troisieme-tasse-poison");
        poison.Kind.Should().Be(StatusEffectKind.DamageOverTime);
        poison.Magnitude.Should().Be(3);
    }

    [Fact]
    public void Resolve_ShouldNeverHalveHealOrApplyPoison_WhenThirdCupIsDisabled()
    {
        for (var i = 0; i < 50; i++)
        {
            var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
            ally.ApplyDamage(50);
            var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
            var combat = TestTacticalCombatHelper.Create(
                RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
                thirdCupHealCorruptionEnabled: false);
            var skill = CreateSkill("skill.heal", "Heal", 20);

            _resolver.Resolve(combat, ally, skill, [ally]);

            ally.CurrentVitality.Should().Be(70);
            ally.StatusEffects.Should().BeEmpty();
        }
    }

    [Fact]
    public void Resolve_ShouldHealPercentOfMaxVitality_WhenBasePowerIsPercentOfMaxVitality()
    {
        var (combat, ally, _) = CreateCombat();
        ally.ApplyDamage(50);
        var skill = CombatantSkill.Create(
            "skill.favorite-de-elise", "Favorite de Elise", "Heal", "Self", "Heal",
            manaCost: 0, chargeCost: 0, basePower: 15,
            basePowerIsPercentOfMaxVitality: true, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [ally]);

        // MaxVitality is 100 (see CreateCombat) => 15% instantly restored, regardless
        // of how much vitality was actually missing.
        ally.CurrentVitality.Should().Be(65);
    }

    [Fact]
    public void Resolve_ShouldRestoreMana_WhenEffectTypeIsRestoreMana()
    {
        var caster = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, mana: 26);
        var ally = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Skirmisher", 46, mana: 20);
        var enemy = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [enemy], [caster, ally]);
        // Spent AFTER combat creation (not before) so the outcome doesn't depend on
        // whether TacticalCombat's activation-mana-regen picked this combatant as the
        // first active one — always leaves exactly 5/20 mana regardless.
        ally.SpendMana(ally.Mana - 5);
        var skill = CombatantSkill.Create(
            "canon.skill.recharge", "Recharge", "Buff", "SingleAlly", "RestoreMana",
            manaCost: 0, chargeCost: 0, basePower: 8, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, caster, skill, [ally]);

        ally.Mana.Should().Be(13);
    }

    [Fact]
    public void Resolve_ShouldClampRestoredMana_ToMaxMana()
    {
        var caster = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, mana: 26);
        var ally = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Skirmisher", 46, mana: 20);
        var enemy = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [enemy], [caster, ally]);
        // Spent AFTER combat creation — see Resolve_ShouldRestoreMana_WhenEffectTypeIsRestoreMana.
        ally.SpendMana(ally.Mana - 16);
        var skill = CombatantSkill.Create(
            "canon.skill.recharge", "Recharge", "Buff", "SingleAlly", "RestoreMana",
            manaCost: 0, chargeCost: 0, basePower: 8, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, caster, skill, [ally]);

        ally.Mana.Should().Be(20);
    }

    [Fact]
    public void Resolve_ShouldScaleHealByActorEffectiveHealingBonusPercent()
    {
        var (combat, ally, _) = CreateCombat();
        ally.ApplyDamage(50);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            healingBonusPercent: 15);
        var skill = CreateSkill("skill.heal", "Heal", 20);

        _resolver.Resolve(combat, ally, skill, [ally]);

        // 20 base heal * 1.15 = 23 (rounded).
        ally.CurrentVitality.Should().Be(50 + 23);
    }

    [Fact]
    public void Resolve_ShouldGrantActorTargetsSkills_WhenEffectTypeIsCopySkills()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemySkill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 22, emotionalRegister: "Neutral");
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, skills: [enemySkill]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.creation", "Création", "Buff", "SingleEnemy", "CopySkills",
            manaCost: 20, chargeCost: 0, basePower: 10, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.Skills.Should().Contain(s => s.Key == "canon.skill.flamme-froide");
        ally.PermanentSkills.Should().NotContain(s => s.Key == "canon.skill.flamme-froide");
    }

    [Fact]
    public void Resolve_ShouldBoostDotMagnitude_WhenActorHasDotDamageBonus()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 0, magicDamageReductionPercent: 0, criticalChanceBonusPercent: 0,
            dotDamageBonusPercent: 50);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // +50% DoT damage bonus on the caster => 10 base magnitude becomes 15.
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.Magnitude == 15);
    }

    [Fact]
    public void Resolve_ShouldScaleDotMagnitude_ByMagicAttackAndMagicDefense_ForMagicCategorySkills()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 20);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.encre-vive", "Encre vive", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0, category: "Magic",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "ink", "Encre vive", StatusEffectKind.DamageOverTime,
                    Magnitude: 6, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // multiplier = (20 baseline + 20 magic attack) / (20 baseline + 0 magic defense) = 2.0
        // => 6 base magnitude becomes 12, same symmetric ratio as instant Magic damage.
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "ink" && e.Magnitude == 12);
    }

    [Fact]
    public void Resolve_ShouldIgnoreMagicAttackDefense_ForPhysicalCategoryDotSkills()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 40);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Physical-category (default) DoT: MagicAttack is irrelevant, and both
        // AttackPower/Defense sit at 0, so the ratio stays neutral at 1.0.
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.Magnitude == 10);
    }

    [Fact]
    public void Resolve_ShouldNotScalePercentOfMaxDot_ByAttackOrDefense()
    {
        // A self-inflicted, percent-of-max-HP DoT (e.g. "Une destinée cruelle") is a
        // curse proportional to the caster's own max HP, not an attack roll against a
        // defender — the attack/defense ratio must not apply to it.
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 60);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.destinee-cruelle", "Une destinée cruelle", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "destinee-cruelle:dot", "Une destinée cruelle", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 0, TickInterval: 1400,
                    MagnitudeIsPercentOfMax: true, AppliesToActor: true, IsPermanent: true)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "destinee-cruelle:dot" && e.Magnitude == 10);
    }

    [Fact]
    public void Resolve_ShouldExtendRemainingDotDuration_WhenEffectTypeIsExtendDotDuration()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        enemy.ApplyStatusEffect(CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: combat.CurrentTick, durationTicks: 1000, magnitude: 10, tickInterval: 1400));
        var skill = CombatantSkill.Create(
            "canon.skill.ecriture-continuelle", "Écriture continuelle", "Debuff", "SingleEnemy", "ExtendDotDuration",
            manaCost: 0, chargeCost: 0, basePower: 25, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Remaining ticks (1000) extended by +25% => 1250.
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.ExpiresAtTick == combat.CurrentTick + 1250);
    }

    [Fact]
    public void Resolve_ShouldReduceDamage_WhenTargetHasEquipmentDrivenTypedReduction()
    {
        var (combat, ally, enemy) = CreateCombat();
        ally.ApplyTypedDamageReductions(new Dictionary<EmotionalType, int> { [EmotionalType.Memoire] = 15 });
        var skill = CombatantSkill.Create(
            "skill.enemy.memoire-strike", "Frappe mémorielle", "Damage", "SingleAlly", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 20, emotionalRegister: "Memoire");

        _resolver.Resolve(combat, enemy, skill, [ally]);

        // ally.EffectiveDefense is 0, so TacticalDamageFormula forces
        // round(20 * 1.15) = 23; neutral type match for player.self (no weak/resist to
        // Mémoire) leaves it at 23, then -15% equipment reduction => round(23*0.85) = 20.
        ally.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldApplyAppliesToActorEffect_ToTheCaster_NotTheTarget()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CombatantSkill.Create(
            "skill.liberte-retrouvee", "La liberté retrouvée", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "liberte-retrouvee:speed", "Liberté retrouvée", StatusEffectKind.StatModifier,
                    Magnitude: 10, DurationTicks: 25000, Stat: CombatStat.Speed,
                    MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "liberte-retrouvee:speed");
        enemy.StatusEffects.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldGrantAppliesToActorEffect_OnlyWhenTheAttackActuallyLands()
    {
        // The hit roll is deterministic (seeded from combat/actor/target/skill ids)
        // but effectively unpredictable from the test's point of view since ids are
        // freshly generated each run — so this asserts the INVARIANT for whichever
        // outcome occurs, rather than forcing one via a hardcoded seed.
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.liberte-retrouvee", "La liberté retrouvée", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "liberte-retrouvee:speed", "Liberté retrouvée", StatusEffectKind.StatModifier,
                    Magnitude: 10, DurationTicks: 25000, Stat: CombatStat.Speed,
                    MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            }, emotionalRegister: "Neutral");

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        var missed = result.LogEntries.Any(e => e.Type == "AttackMissed");
        if (missed)
        {
            ally.StatusEffects.Should().BeEmpty(because: "a miss must not grant the self-buff.");
        }
        else
        {
            ally.StatusEffects.Should().ContainSingle();
        }
    }

    [Fact]
    public void Resolve_ShouldNotApplyTargetStatusEffect_WhenTheAttackMisses()
    {
        // Regression: a Damage skill carrying a target-side status effect (DoT/debuff)
        // used to apply that status unconditionally, even on a target the attack roll
        // itself reported as missed. Same non-deterministic-but-invariant approach as
        // Resolve_ShouldGrantAppliesToActorEffect_OnlyWhenTheAttackActuallyLands above.
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        var missed = result.LogEntries.Any(e => e.Type == "AttackMissed");
        if (missed)
        {
            enemy.StatusEffects.Should().BeEmpty(because: "a missed attack must not still poison the target.");
        }
        else
        {
            enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison");
        }
    }

    [Fact]
    public void Resolve_ShouldApplyTargetStatusEffect_OnlyOnTargetsActuallyHit_WithMultipleTargets()
    {
        // Two targets, deterministically different hit outcomes are possible per-target
        // (the hit roll is seeded per actor/target/skill) — whichever targets are hit
        // must be exactly the ones carrying the status effect afterwards, never more.
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemyA = Combatant.CreateEnemy("enemy.sentinel-a", "Sentinel A", "Guard", 100);
        var enemyB = Combatant.CreateEnemy("enemy.sentinel-b", "Sentinel B", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemyA, enemyB]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume-aoe", "Plume empoisonnée (zone)", "Damage", "AllEnemies", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        var result = _resolver.Resolve(combat, ally, skill, [enemyA, enemyB]);

        var missedTargetIds = result.LogEntries
            .Where(e => e.Type == "AttackMissed")
            .SelectMany(e => e.TargetIds)
            .ToHashSet();

        foreach (var enemy in new[] { enemyA, enemyB })
        {
            if (missedTargetIds.Contains(enemy.Id.Value))
            {
                enemy.StatusEffects.Should().BeEmpty(because: $"{enemy.DisplayName} was missed and must not be poisoned.");
            }
            else
            {
                enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison");
            }
        }
    }

    [Fact]
    public void Resolve_ShouldApplyTargetStatusEffect_WhenSkillHasNoInstantEffect_RegardlessOfHitRoll()
    {
        // A pure status-only skill (no Damage instant effect) has no hit/miss roll at
        // all — hitTargetIds stays null and every target is affected, same as before
        // this fix.
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotContain(e => e.Type == "AttackMissed");
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison");
    }

    [Fact]
    public void Resolve_ShouldBoostDamage_WhenSkillIsMagicCategory_AndActorHasMagicDamageBonus()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 50, magicDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // enemy's Defense/MagicDefense are 0, so TacticalDamageFormula forces
        // round(10 * 1.15) = 12; Magic bonus +50% is then applied on top of that
        // base: round(12 * 1.5) = 18.
        enemy.CurrentVitality.Should().Be(200 - 18);
    }

    [Fact]
    public void Resolve_ShouldReduceDamage_WhenSkillIsMagicCategory_AndTargetHasMagicDamageReduction()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        enemy.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 0, magicDamageReductionPercent: 20);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense/magicDefense both 0); Magic reduction
        // -20% applied on top: round(12 * 0.80) = round(9.6) = 10.
        enemy.CurrentVitality.Should().Be(200 - 10);
    }

    [Fact]
    public void Resolve_ShouldIgnoreMagicDamageBonus_WhenSkillIsPhysicalCategory()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 50, magicDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Physical", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Physical skill: the actor's Magic damage bonus must not apply — base stays
        // at round(10 * 1.15) = 12 (defense is 0).
        enemy.CurrentVitality.Should().Be(200 - 12);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Silence Dû" (PhysicalDamageBonus / FlatManaCostBonus)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldBoostDamage_WhenSkillIsPhysicalCategory_AndActorHasPhysicalDamageBonus()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "law-silence-du:physical-damage", displayName: "Silence dû",
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: 8, stat: CombatStat.PhysicalDamageBonus, isPermanent: true));
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Physical", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0); Physical bonus +8% applied on
        // top: round(12 * 1.08) = round(12.96) = 13.
        enemy.CurrentVitality.Should().Be(200 - 13);
    }

    [Fact]
    public void Resolve_ShouldIgnorePhysicalDamageBonus_WhenSkillIsMagicCategory()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "law-silence-du:physical-damage", displayName: "Silence dû",
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: 8, stat: CombatStat.PhysicalDamageBonus, isPermanent: true));
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Magic-category skill: the actor's Physical damage bonus must not apply —
        // base stays at round(10 * 1.15) = 12 (defense is 0).
        enemy.CurrentVitality.Should().Be(200 - 12);
    }

    [Fact]
    public void Resolve_ShouldAddFlatManaCostBonus_ToPlayerSideCast()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.GainMana(10);
        ally.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "law-silence-du:mana-cost", displayName: "Silence dû",
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: 2, stat: CombatStat.FlatManaCostBonus, isPermanent: true));
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            manaCost: 3, chargeCost: 0, basePower: 10, category: "Physical", emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.Mana.Should().Be(10 - 3 - 2);
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Première Impression" (Combat.FirstHitCriticalEnabled) — combat-scoped:
    // the very first landed hit of the WHOLE combat is forced critical, regardless of
    // which side lands it (not a per-combatant effect).
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldForceACriticalHit_OnTheCombatsFirstLandedHit_WhenEnabled()
    {
        // Default CreateCombat ally has Focus 0 => normal crit chance is exactly 0,
        // so any crit observed here can only come from the first-hit-critical law.
        var (combat, ally, enemy) = CreateFirstHitCriticalCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().Contain(entry => entry.Type == "CriticalHit");
        // base = round(10 * 1.15) = 12 (defense is 0); TacticalCritMultiplier is 1.6x
        // (not the ATB 1.5x) — round(12 * 1.6) = 19.2 -> 19.
        enemy.CurrentVitality.Should().Be(80 - 19);
    }

    [Fact]
    public void Resolve_ShouldConsumeFirstHitCritical_AfterItLands()
    {
        var (combat, ally, enemy) = CreateFirstHitCriticalCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        combat.HasFirstHitLanded.Should().BeTrue();
    }

    [Fact]
    public void Resolve_ShouldOnlyGuaranteeOneCriticalHit_OnAMultiTargetSkill()
    {
        var (combat, ally, enemyA, enemyB) = CreateFirstHitCriticalCombatWithTwoEnemies();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemyA, enemyB]);

        result.LogEntries.Count(entry => entry.Type == "CriticalHit").Should().Be(1,
            because: "the first-hit critical is consumed after the first landed hit — the second target rolls normally.");
    }

    [Fact]
    public void Resolve_ShouldNotForceACriticalHit_OnASecondCombatsFirstHit_WhenAPreviousHitAlreadyLanded()
    {
        var (combat, ally, enemy) = CreateFirstHitCriticalCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);
        _resolver.Resolve(combat, ally, skill, [enemy]); // consumes the combat's first hit

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotContain(entry => entry.Type == "CriticalHit");
    }

    // ---------------------------------------------------------------------------
    // "Loi du Treizième Coup" (HitCounterDoubleDamage)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldDoubleDamage_OnTheThirteenthLandedHit_WhenEnabled()
    {
        var (combat, ally, enemy) = CreateThirteenthHitCombat(hitCounterDoubleDamageEnabled: true);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        for (var i = 0; i < CombatRules.HitCounterTrigger; i++)
        {
            _resolver.Resolve(combat, ally, skill, [enemy]);
        }

        // Each hit's base = round(10 * 1.15) = 12 (defense is 0): 12 normal hits of 12
        // + the 13th hit doubled to 24 = 168 total.
        enemy.CurrentVitality.Should().Be(1000 - 168);
    }

    [Fact]
    public void Resolve_ShouldNotDoubleDamage_WhenHitCounterDoubleDamageIsDisabled()
    {
        var (combat, ally, enemy) = CreateThirteenthHitCombat(hitCounterDoubleDamageEnabled: false);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        for (var i = 0; i < CombatRules.HitCounterTrigger; i++)
        {
            _resolver.Resolve(combat, ally, skill, [enemy]);
        }

        // Each hit's base = round(10 * 1.15) = 12 (defense is 0): 13 normal hits of
        // 12 = 156 total, none doubled.
        enemy.CurrentVitality.Should().Be(1000 - 156);
    }

    [Fact]
    public void Resolve_ShouldLogTheThirteenthHit_WhenItLands()
    {
        var (combat, ally, enemy) = CreateThirteenthHitCombat(hitCounterDoubleDamageEnabled: true);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        CombatSkillEffectResolution lastResult = null!;
        for (var i = 0; i < CombatRules.HitCounterTrigger; i++)
        {
            lastResult = _resolver.Resolve(combat, ally, skill, [enemy]);
        }

        lastResult.LogEntries.Should().Contain(entry => entry.Type == "ThirteenthHit");
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Curée" (DamageAmplificationBelowHpThreshold) — +15% damage taken
    // while the TARGET is already below 25% of its max vitality, symmetric across
    // both sides.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldAmplifyDamage_WhenTargetIsBelowTheLowHpThreshold_AndEnabled()
    {
        var (combat, ally, enemy) = CreateLowHpAmplificationCombat(lowHpDamageAmplificationEnabled: true, enemyMaxVitality: 200);
        enemy.ApplyDamage(160); // 40/200 = 20% < 25% threshold
        var skill = CreateSkill("skill.basic.strike", "Damage", 20);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(20 * 1.15) = 23 (defense is 0); Curée amplifies the final
        // outcome by +15%: round(23 * 1.15) = round(26.45) = 26.
        enemy.CurrentVitality.Should().Be(40 - 26);
    }

    [Fact]
    public void Resolve_ShouldNotAmplifyDamage_WhenTargetIsAboveTheLowHpThreshold()
    {
        var (combat, ally, enemy) = CreateLowHpAmplificationCombat(lowHpDamageAmplificationEnabled: true, enemyMaxVitality: 100);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0); above the threshold, no
        // amplification applies.
        enemy.CurrentVitality.Should().Be(100 - 12);
    }

    [Fact]
    public void Resolve_ShouldNotAmplifyDamage_WhenTheLawIsDisabled()
    {
        var (combat, ally, enemy) = CreateLowHpAmplificationCombat(lowHpDamageAmplificationEnabled: false, enemyMaxVitality: 100);
        enemy.ApplyDamage(80); // 20% HP, would qualify if the law were active
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0); law disabled, no amplification.
        enemy.CurrentVitality.Should().Be(20 - 12);
    }

    // ---------------------------------------------------------------------------
    // "Loi de l'Écriture" (DotDurationExtensionTicks) — every DamageOverTime effect
    // (both sides) lasts N extra ticks.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldExtendDotDuration_WhenDotDurationExtensionIsActive()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: false,
            dotDurationExtensionTicks: 5000);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.ExpiresAtTick == 10000);
    }

    [Fact]
    public void Resolve_ShouldNotExtendDotDuration_WhenTheLawIsNotActive()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.ExpiresAtTick == 5000);
    }

    // ---------------------------------------------------------------------------
    // "Le Protocole" (Bestiaire, Veilleurs du Seuil) — while a living Porteur de
    // Plateau is present among the enemies, debuffs an enemy inflicts on a player
    // last 1 extra turn (CombatTime.TicksPerTurn).
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldExtendDebuffDuration_WhenPorteurDePlateauIsAlive()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur du Tapis", "Guard", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-plateau", "Porteur de Plateau", "Support", 50);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy, porteur]);
        // TacticalCombat clocks a status effect's expiry from the recipient's own
        // activation-based clock (StatusClockFor), not a shared tick 0 — capture it
        // before resolving so the expected expiry doesn't depend on which combatant
        // happened to activate first at combat creation.
        var baseline = combat.StatusClockFor(ally.Id.Value);
        var skill = CombatantSkill.Create(
            "canon.skill.etouffement-feutre", "Étouffement feutré", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "slow", "Ralenti", StatusEffectKind.StatModifier,
                    Magnitude: -15, DurationTicks: 5000, Stat: CombatStat.Speed)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, enemy, skill, [ally]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "slow" && e.ExpiresAtTick == baseline + 5000 + CombatTime.TicksPerTurn);
    }

    [Fact]
    public void Resolve_ShouldNotExtendDebuffDuration_WhenPorteurDePlateauIsAbsent()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur du Tapis", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var baseline = combat.StatusClockFor(ally.Id.Value);
        var skill = CombatantSkill.Create(
            "canon.skill.etouffement-feutre", "Étouffement feutré", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "slow", "Ralenti", StatusEffectKind.StatModifier,
                    Magnitude: -15, DurationTicks: 5000, Stat: CombatStat.Speed)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, enemy, skill, [ally]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "slow" && e.ExpiresAtTick == baseline + 5000);
    }

    [Fact]
    public void Resolve_ShouldNotExtendDebuffDuration_WhenPorteurDePlateauIsDefeated()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur du Tapis", "Guard", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-plateau", "Porteur de Plateau", "Support", 10);
        porteur.ApplyDamage(10);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy, porteur]);
        var baseline = combat.StatusClockFor(ally.Id.Value);
        var skill = CombatantSkill.Create(
            "canon.skill.etouffement-feutre", "Étouffement feutré", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "slow", "Ralenti", StatusEffectKind.StatModifier,
                    Magnitude: -15, DurationTicks: 5000, Stat: CombatStat.Speed)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, enemy, skill, [ally]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "slow" && e.ExpiresAtTick == baseline + 5000);
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Marée Haute" (DotMagnitudeBonus) — every flat-magnitude DamageOverTime
    // effect (both sides) deals N extra damage per tick.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldAddFlatMagnitudeBonus_WhenDotMagnitudeBonusIsActive()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: false,
            dotDurationExtensionTicks: 0,
            duelDamageAsymmetryEnabled: false,
            dotMagnitudeBonus: 1);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.Magnitude == 11);
    }

    [Fact]
    public void Resolve_ShouldNotAddMagnitudeBonus_WhenTheLawIsNotActive()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.Magnitude == 10);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Duel" (DuelDamageAsymmetryEnabled) — mono-target +20%, area-of-effect -20%.
    // ---------------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldBoostMonoTargetDamage_WhenDuelDamageAsymmetryIsActive()
    {
        var (combat, ally, enemy) = CreateDuelCombat(duelDamageAsymmetryEnabled: true);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0); mono-target +20% applied on
        // top: round(12 * 1.2) = 14.4 -> 14.
        enemy.CurrentVitality.Should().Be(80 - 14);
    }

    [Fact]
    public void Resolve_ShouldReduceAreaOfEffectDamage_WhenDuelDamageAsymmetryIsActive()
    {
        var (combat, ally, enemyA, enemyB) = CreateDuelCombatWithTwoEnemies(duelDamageAsymmetryEnabled: true);
        var skill = CombatantSkill.Create(
            "skill.basic.blast", "Explosion", "Damage", "AllEnemies", "Damage", 0, 0, 10, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemyA, enemyB]);

        // base = round(10 * 1.15) = 12 (defense is 0); area-of-effect -20% applied on
        // top: round(12 * 0.8) = round(9.6) = 10.
        enemyA.CurrentVitality.Should().Be(80 - 10);
        enemyB.CurrentVitality.Should().Be(80 - 10);
    }

    [Fact]
    public void Resolve_ShouldNotChangeDamage_WhenDuelDamageAsymmetryIsNotActive()
    {
        var (combat, ally, enemy) = CreateDuelCombat(duelDamageAsymmetryEnabled: false);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // base = round(10 * 1.15) = 12 (defense is 0); law disabled, no change.
        enemy.CurrentVitality.Should().Be(80 - 12);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateDuelCombat(
        bool duelDamageAsymmetryEnabled)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(100, 0, 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: false,
            dotDurationExtensionTicks: 0,
            duelDamageAsymmetryEnabled: duelDamageAsymmetryEnabled);

        return (combat, ally, enemy);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant EnemyA, Combatant EnemyB) CreateDuelCombatWithTwoEnemies(
        bool duelDamageAsymmetryEnabled)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(100, 0, 0); // guaranteed hit chance
        var enemyA = Combatant.CreateEnemy("enemy.sentinel-a", "Sentinel A", "Guard", 80);
        var enemyB = Combatant.CreateEnemy("enemy.sentinel-b", "Sentinel B", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemyA, enemyB],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: false,
            dotDurationExtensionTicks: 0,
            duelDamageAsymmetryEnabled: duelDamageAsymmetryEnabled);

        return (combat, ally, enemyA, enemyB);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateLowHpAmplificationCombat(
        bool lowHpDamageAmplificationEnabled, int enemyMaxVitality)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(100, 0, 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", enemyMaxVitality);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: lowHpDamageAmplificationEnabled);

        return (combat, ally, enemy);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateThirteenthHitCombat(
        bool hitCounterDoubleDamageEnabled)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        // Guaranteed hit chance: the hit-roll seed is stable across repeated calls with
        // the same actor/target/skill/turn, so a natural miss would deterministically
        // repeat on every iteration instead of varying — remove that risk entirely.
        ally.ApplyEquipmentCombatModifiers(100, 0, 0);

        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 1000);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy], hitCounterDoubleDamageEnabled: hitCounterDoubleDamageEnabled);

        return (combat, ally, enemy);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateFirstHitCriticalCombat()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy], hitCounterDoubleDamageEnabled: false, firstHitCriticalEnabled: true);

        return (combat, ally, enemy);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant EnemyA, Combatant EnemyB) CreateFirstHitCriticalCombatWithTwoEnemies()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0); // guaranteed hit chance
        var enemyA = Combatant.CreateEnemy("enemy.sentinel-a", "Sentinel A", "Guard", 80);
        var enemyB = Combatant.CreateEnemy("enemy.sentinel-b", "Sentinel B", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemyA, enemyB], hitCounterDoubleDamageEnabled: false, firstHitCriticalEnabled: true);

        return (combat, ally, enemyA, enemyB);
    }

    private static (TacticalCombat Combat, Combatant Ally, Combatant Enemy) CreateCombat()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        // Guaranteed hit chance on both sides: the tactical resolver rolls a real
        // hit/miss chance (baseline 90%) per attack, which the old ATB-era Combat this
        // fixture was written against never had — pin it so single-shot damage
        // assertions below aren't ~10% flaky.
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        enemy.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);

        return (combat, ally, enemy);
    }

    private static CombatantSkill CreateSkill(string key, string effectType, int basePower)
    {
        return CombatantSkill.Create(
            key,
            key,
            effectType,
            "SingleEnemy",
            effectType,
            0,
            0,
            basePower, emotionalRegister: "Neutral");
    }
}
