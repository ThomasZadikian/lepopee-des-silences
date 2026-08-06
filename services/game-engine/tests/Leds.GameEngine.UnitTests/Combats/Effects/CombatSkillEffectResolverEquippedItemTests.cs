using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

/// <summary>
/// Three accessories whose effect targets what the wearer applies TO OTHERS rather than
/// the wearer's own stats — resolved via a hardcoded equipped-item-key check in
/// CombatSkillEffectResolver (same convention as Cornes d'ivoire/Diapason de l'au-delà)
/// instead of the generic ItemEquipmentEffect pipeline.
/// </summary>
public sealed class CombatSkillEffectResolverEquippedItemTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    [Fact]
    public void Resolve_ShouldExtendHostileEffectDuration_WhenCasterHasEpingleDuProtocole()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:hostile-status-duration-plus-one"]
            });
        var skill = CombatantSkill.Create(
            "canon.skill.affaiblissement", "Affaiblissement", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "weaken", "Affaiblissement", StatusEffectKind.StatModifier,
                    Magnitude: -10, DurationTicks: 2500, Stat: CombatStat.AttackPower)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "weaken" && e.ExpiresAtTick == 5000);
    }

    [Fact]
    public void Resolve_ShouldNotExtendHostileEffectDuration_WithoutEpingleDuProtocole()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var skill = CombatantSkill.Create(
            "canon.skill.affaiblissement", "Affaiblissement", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0,
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "weaken", "Affaiblissement", StatusEffectKind.StatModifier,
                    Magnitude: -10, DurationTicks: 2500, Stat: CombatStat.AttackPower)
            }, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "weaken" && e.ExpiresAtTick == 2500);
    }

    [Fact]
    public void Resolve_ShouldExtendFirstDotApplication_WhenCasterHasEncrierDePoche()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:first-dot-duration-plus-one"]
            });
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

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "poison" && e.ExpiresAtTick == 7500);
    }

    [Fact]
    public void Resolve_ShouldReduceSilenceManaCost_WhenCasterHasGrainDuChoeur()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 20, charge: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:silence-mana-minus-two", "behavior:silence-duration-plus-one"]
            });
        var skill = CombatantSkill.Create(
            "canon.skill.silence", "Silence", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 5, chargeCost: 0, basePower: 0, emotionalRegister: "Silence",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "silence", "Silence", StatusEffectKind.Silence, Magnitude: 0, DurationTicks: 2500)
            });

        _resolver.Resolve(combat, ally, skill, [enemy]);

        // Base mana cost 5, -2 flat from Grain du chœur (register:Silence) => 3 spent.
        ally.Mana.Should().Be(17);
    }

    [Fact]
    public void Resolve_ShouldNotReduceManaCost_ForNonSilenceRegisterSkills_EvenWithGrainDuChoeur()
    {
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 20, charge: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:silence-mana-minus-two", "behavior:silence-duration-plus-one"]
            });
        var skill = CombatantSkill.Create(
            "canon.skill.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            manaCost: 5, chargeCost: 0, basePower: 10, emotionalRegister: "Neutral");

        _resolver.Resolve(combat, ally, skill, [enemy]);

        ally.Mana.Should().Be(15);
    }

    [Fact]
    public void Resolve_ShouldCastWearersUltimateForFree_OnDeath_WhenWearerHasDiapasonDeLAuDela()
    {
        var ultimate = CombatantSkill.Create(
            "canon.skill.ultimate", "Ultime", "Damage", "SingleEnemy", "Damage",
            manaCost: 999, chargeCost: 999, basePower: 10, isUltimate: true, emotionalRegister: "Neutral");
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 30, currentVitality: 30, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            skills: [ultimate]);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        enemy.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:prevent-revive-signature-on-death"]
            });
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", 100);

        _resolver.Resolve(combat, enemy, lethalSkill, [ally]);

        ally.Status.Should().Be(CombatantStatus.Defeated);
        // The ultimate's manaCost/chargeCost (999/999) would normally be unaffordable —
        // proof the free-cast bypasses ConsumeResources entirely, per
        // "ignore-all-costs-and-cooldown". A defenseless enemy forces the 115% variation:
        // round(10 * 1.15) = 12.
        enemy.CurrentVitality.Should().Be(88);
    }

    [Fact]
    public void Resolve_ShouldNotCastAnything_OnDeath_WithoutDiapasonDeLAuDela()
    {
        var ultimate = CombatantSkill.Create(
            "canon.skill.ultimate", "Ultime", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10, isUltimate: true, emotionalRegister: "Neutral");
        var ally = Combatant.Create(
            CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter",
            maxVitality: 30, currentVitality: 30, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            skills: [ultimate]);
        ally.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        enemy.ApplyEquipmentCombatModifiers(hitChanceBonusPercent: 100, dotDurationReductionPercent: 0, dotDamageReductionPercent: 0);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", 100);

        _resolver.Resolve(combat, enemy, lethalSkill, [ally]);

        ally.Status.Should().Be(CombatantStatus.Defeated);
        enemy.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void Resolve_ShouldExtendFirstSilenceApplication_WhenCasterHasGrainDuChoeur()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:silence-mana-minus-two", "behavior:silence-duration-plus-one"]
            });
        var skill = CombatantSkill.Create(
            "canon.skill.silence", "Silence", "Debuff", "SingleEnemy", "Debuff",
            manaCost: 0, chargeCost: 0, basePower: 0, emotionalRegister: "Silence",
            statusEffects: new[]
            {
                new SkillStatusEffectSpec(
                    "silence", "Silence", StatusEffectKind.Silence, Magnitude: 0, DurationTicks: 2500)
            });

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.StatusEffects.Should().ContainSingle(e => e.Key == "silence" && e.ExpiresAtTick == 5000);
    }

    private static CombatantSkill CreateSkill(string key, string effectType, int basePower)
    {
        return CombatantSkill.Create(
            key, key, effectType, "SingleEnemy", effectType, 0, 0, basePower, emotionalRegister: "Neutral");
    }
}
