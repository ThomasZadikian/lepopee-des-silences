using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

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
        enemy.CurrentVitality.Should().Be(70);
    }

    [Fact]
    public void Resolve_ShouldApplyDamage_WhenEffectTypeIsDamageVitality()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "DamageVitality", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotBeEmpty();
        enemy.CurrentVitality.Should().Be(70);
    }

    [Fact]
    public void Resolve_ShouldConsumeGuardBeforeVitality_WhenDamageIsApplied()
    {
        var (combat, ally, enemy) = CreateCombat();
        enemy.GainGuard(5);
        var skill = CreateSkill("skill.basic.strike", "Damage", 12);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.Guard.Should().Be(0);
        enemy.CurrentVitality.Should().Be(73);
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
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // multiplier = (20 baseline + 20 attack) / (20 baseline + 0 defense) = 2.0.
        enemy.CurrentVitality.Should().Be(80 - 20);
    }

    [Fact]
    public void Resolve_ShouldReduceDamage_WhenTargetDefenseIsAboveBaseline()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 20);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // multiplier = (20 baseline + 0 attack) / (20 baseline + 20 defense) = 0.5.
        enemy.CurrentVitality.Should().Be(80 - 5);
    }

    [Fact]
    public void Resolve_ShouldIncreaseMagicDamage_UsingMagicAttackNotPhysicalAttackPower()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            attackPower: 20, magicAttack: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // A high physical AttackPower must NOT boost a Magic-category skill: the
        // physical stat is ignored here, only MagicAttack (0) applies, so the ratio
        // stays neutral — (20 baseline + 0 magic attack) / (20 baseline + 0 magic
        // defense) = 1.0.
        enemy.CurrentVitality.Should().Be(80 - 10);
    }

    [Fact]
    public void Resolve_ShouldIncreaseMagicDamage_WhenActorMagicAttackIsAboveBaseline()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 20);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // multiplier = (20 baseline + 20 magic attack) / (20 baseline + 0 magic defense) = 2.0.
        enemy.CurrentVitality.Should().Be(80 - 20);
    }

    [Fact]
    public void Resolve_ShouldReduceMagicDamage_WhenTargetMagicDefenseIsAboveBaseline()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 20);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 8, chargeCost: 0, basePower: 10, category: "Magic");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // multiplier = (20 baseline + 0 magic attack) / (20 baseline + 20 magic defense) = 0.5.
        enemy.CurrentVitality.Should().Be(80 - 5);
    }

    [Fact]
    public void Resolve_ShouldIgnoreMagicAttackDefense_ForPhysicalCategorySkills()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicAttack: 30);
        var enemy = Combatant.Create(
            CombatantId.New(), "enemy.sentinel", "Sentinel", CombatantSide.Enemy, "Guard",
            maxVitality: 80, currentVitality: 80, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 30);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Physical-category skill: MagicAttack/MagicDefense are irrelevant, ratio
        // stays neutral at (20 baseline + 0 attack) / (20 baseline + 0 defense) = 1.0.
        enemy.CurrentVitality.Should().Be(70);
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

        act.Should().Throw<DomainException>().WithMessage("Unsupported skill effect type: Unknown");
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

    [Fact]
    public void Resolve_ShouldHealPercentOfMaxVitality_WhenBasePowerIsPercentOfMaxVitality()
    {
        var (combat, ally, _) = CreateCombat();
        ally.ApplyDamage(50);
        var skill = CombatantSkill.Create(
            "skill.favorite-de-elise", "Favorite de Elise", "Heal", "Self", "Heal",
            manaCost: 0, chargeCost: 0, basePower: 15,
            basePowerIsPercentOfMaxVitality: true);

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
        ally.SpendMana(15); // 5/20 mana remaining
        var enemy = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [enemy], [caster, ally]);
        var skill = CombatantSkill.Create(
            "canon.skill.recharge", "Recharge", "Buff", "SingleAlly", "RestoreMana",
            manaCost: 0, chargeCost: 0, basePower: 8, category: "Magic");

        _resolver.Resolve(combat, caster, skill, [ally]);

        ally.Mana.Should().Be(13);
    }

    [Fact]
    public void Resolve_ShouldClampRestoredMana_ToMaxMana()
    {
        var caster = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, mana: 26);
        var ally = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Skirmisher", 46, mana: 20);
        ally.SpendMana(4); // 16/20 mana remaining
        var enemy = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [enemy], [caster, ally]);
        var skill = CombatantSkill.Create(
            "canon.skill.recharge", "Recharge", "Buff", "SingleAlly", "RestoreMana",
            manaCost: 0, chargeCost: 0, basePower: 8, category: "Magic");

        _resolver.Resolve(combat, caster, skill, [ally]);

        ally.Mana.Should().Be(20);
    }

    [Fact]
    public void Resolve_ShouldScaleHealByActorEffectiveHealingBonusPercent()
    {
        var (combat, ally, _) = CreateCombat();
        ally.ApplyDamage(50);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
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
            manaCost: 8, chargeCost: 0, basePower: 22);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, skills: [enemySkill]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.creation", "Création", "Buff", "SingleEnemy", "CopySkills",
            manaCost: 20, chargeCost: 0, basePower: 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.Skills.Should().Contain(s => s.Key == "canon.skill.flamme-froide");
        ally.PermanentSkills.Should().NotContain(s => s.Key == "canon.skill.flamme-froide");
    }

    [Fact]
    public void Resolve_ShouldBoostDotMagnitude_WhenActorHasDotDamageBonus()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 0, magicDamageReductionPercent: 0, criticalChanceBonusPercent: 0,
            dotDamageBonusPercent: 50);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.encre-vive", "Encre vive", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0, category: "Magic",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "ink", "Encre vive", StatusEffectKind.DamageOverTime,
                    Magnitude: 6, DurationTicks: 5000, TickInterval: 1400)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.destinee-cruelle", "Une destinée cruelle", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "destinee-cruelle:dot", "Une destinée cruelle", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 0, TickInterval: 1400,
                    MagnitudeIsPercentOfMax: true, AppliesToActor: true, IsPermanent: true)
            });

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.StatusEffects.Should().ContainSingle(e => e.Key == "destinee-cruelle:dot" && e.Magnitude == 10);
    }

    [Fact]
    public void Resolve_ShouldExtendRemainingDotDuration_WhenEffectTypeIsExtendDotDuration()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        enemy.ApplyStatusEffect(CombatStatusEffect.Create(
            "poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: combat.CurrentTick, durationTicks: 1000, magnitude: 10, tickInterval: 1400));
        var skill = CombatantSkill.Create(
            "canon.skill.ecriture-continuelle", "Écriture continuelle", "Debuff", "SingleEnemy", "ExtendDotDuration",
            manaCost: 0, chargeCost: 0, basePower: 25);

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
            manaCost: 0, chargeCost: 0, basePower: 20, tags: new[] { "emotype:memoire" });

        _resolver.Resolve(combat, enemy, skill, [ally]);

        // Neutral type match for player.self (no weak/resist to Mémoire) => 20 raw,
        // then -15% equipment reduction => 17 (rounded).
        ally.CurrentVitality.Should().Be(83);
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
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.liberte-retrouvee", "La liberté retrouvée", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "liberte-retrouvee:speed", "Liberté retrouvée", StatusEffectKind.StatModifier,
                    Magnitude: 10, DurationTicks: 25000, Stat: CombatStat.Speed,
                    MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemyA, enemyB]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume-aoe", "Plume empoisonnée (zone)", "Damage", "AllEnemies", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            });

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
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.plume", "Plume empoisonnée", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "poison", "Poison", StatusEffectKind.DamageOverTime,
                    Magnitude: 10, DurationTicks: 5000, TickInterval: 1400)
            });

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().NotContain(e => e.Type == "AttackMissed");
        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison");
    }

    [Fact]
    public void Resolve_ShouldBoostDamage_WhenSkillIsMagicCategory_AndActorHasMagicDamageBonus()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 50, magicDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // baseline multiplier = (20 + 0) / (20 + 0) = 1.0; Magic bonus = +50% => 15 damage.
        enemy.CurrentVitality.Should().Be(200 - 15);
    }

    [Fact]
    public void Resolve_ShouldReduceDamage_WhenSkillIsMagicCategory_AndTargetHasMagicDamageReduction()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        enemy.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 0, magicDamageReductionPercent: 20);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.flamme-froide", "Flamme froide", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Magic");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // baseline multiplier = 1.0; Magic reduction = -20% => 8 damage.
        enemy.CurrentVitality.Should().Be(200 - 8);
    }

    [Fact]
    public void Resolve_ShouldIgnoreMagicDamageBonus_WhenSkillIsPhysicalCategory()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent: 0, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0,
            magicDamageBonusPercent: 50, magicDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 200);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, category: "Physical");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Physical skill: the actor's Magic damage bonus must not apply => raw 10 damage.
        enemy.CurrentVitality.Should().Be(200 - 10);
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
        // CritMultiplier is 1.5x — 10 base power becomes 15.
        enemy.CurrentVitality.Should().Be(80 - 15);
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

        for (var i = 0; i < Combat.HitCounterTrigger; i++)
        {
            _resolver.Resolve(combat, ally, skill, [enemy]);
        }

        // 12 normal hits of 10 + the 13th hit doubled to 20 = 140 total.
        enemy.CurrentVitality.Should().Be(1000 - 140);
    }

    [Fact]
    public void Resolve_ShouldNotDoubleDamage_WhenHitCounterDoubleDamageIsDisabled()
    {
        var (combat, ally, enemy) = CreateThirteenthHitCombat(hitCounterDoubleDamageEnabled: false);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        for (var i = 0; i < Combat.HitCounterTrigger; i++)
        {
            _resolver.Resolve(combat, ally, skill, [enemy]);
        }

        // 13 normal hits of 10 = 130 total, none doubled.
        enemy.CurrentVitality.Should().Be(1000 - 130);
    }

    [Fact]
    public void Resolve_ShouldLogTheThirteenthHit_WhenItLands()
    {
        var (combat, ally, enemy) = CreateThirteenthHitCombat(hitCounterDoubleDamageEnabled: true);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        CombatSkillEffectResolution lastResult = null!;
        for (var i = 0; i < Combat.HitCounterTrigger; i++)
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

        // 20 base damage amplified by +15% = 23.
        enemy.CurrentVitality.Should().Be(40 - 23);
    }

    [Fact]
    public void Resolve_ShouldNotAmplifyDamage_WhenTargetIsAboveTheLowHpThreshold()
    {
        var (combat, ally, enemy) = CreateLowHpAmplificationCombat(lowHpDamageAmplificationEnabled: true, enemyMaxVitality: 100);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(100 - 10);
    }

    [Fact]
    public void Resolve_ShouldNotAmplifyDamage_WhenTheLawIsDisabled()
    {
        var (combat, ally, enemy) = CreateLowHpAmplificationCombat(lowHpDamageAmplificationEnabled: false, enemyMaxVitality: 100);
        enemy.ApplyDamage(80); // 20% HP, would qualify if the law were active
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(20 - 10);
    }

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateLowHpAmplificationCombat(
        bool lowHpDamageAmplificationEnabled, int enemyMaxVitality)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        ally.ApplyEquipmentCombatModifiers(100, 0, 0); // guaranteed hit chance
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", enemyMaxVitality);
        var combat = Combat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy],
            hitCounterDoubleDamageEnabled: false,
            firstHitCriticalEnabled: false,
            lowHpDamageAmplificationEnabled: lowHpDamageAmplificationEnabled);

        return (combat, ally, enemy);
    }

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateThirteenthHitCombat(
        bool hitCounterDoubleDamageEnabled)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        // Guaranteed hit chance: the hit-roll seed is stable across repeated calls with
        // the same actor/target/skill/turn, so a natural miss would deterministically
        // repeat on every iteration instead of varying — remove that risk entirely.
        ally.ApplyEquipmentCombatModifiers(100, 0, 0);

        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 1000);
        var combat = Combat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy], hitCounterDoubleDamageEnabled);

        return (combat, ally, enemy);
    }

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateFirstHitCriticalCombat()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemy], hitCounterDoubleDamageEnabled: false, firstHitCriticalEnabled: true);

        return (combat, ally, enemy);
    }

    private static (Combat Combat, Combatant Ally, Combatant EnemyA, Combatant EnemyB) CreateFirstHitCriticalCombatWithTwoEnemies()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemyA = Combatant.CreateEnemy("enemy.sentinel-a", "Sentinel A", "Guard", 80);
        var enemyB = Combatant.CreateEnemy("enemy.sentinel-b", "Sentinel B", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(),
            [ally], [enemyA, enemyB], hitCounterDoubleDamageEnabled: false, firstHitCriticalEnabled: true);

        return (combat, ally, enemyA, enemyB);
    }

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateCombat()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(),
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
            basePower);
    }
}
