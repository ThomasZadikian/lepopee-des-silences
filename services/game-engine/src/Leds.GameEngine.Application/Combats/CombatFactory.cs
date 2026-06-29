using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats;

public sealed class CombatFactory : ICombatFactory
{
    private const int EnemyVitalityBase = 40;
    private const int VitalityPerDifficulty = 50;

    private readonly EnemyStatScaler _enemyStatScaler = new();

    public Combat CreateFromDraft(
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        int focus = 0,
        IReadOnlyDictionary<string, SkillStatusEffectSpec>? skillEffects = null)
    {
        return CreateFromDraft(
            CombatId.New(),
            draft,
            playerState,
            runModifiers,
            attackPower,
            defense,
            speed,
            palaceRoomState,
            focus,
            skillEffects);
    }
    private static (double VitalityMultiplier, double PowerMultiplier, int GuardBonus) EncounterBonus(string encounterType)
    {
        return encounterType switch
        {
            "RoomBoss" => (2.6, 1.6, 18),
            "Elite" => (1.7, 1.3, 8),
            "Rare" => (1.4, 1.25, 4),
            _ => (1.0, 1.0, 0)
        };
    }


    public Combat CreateFromDraft(
        CombatId combatId,
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        int focus = 0,
        IReadOnlyDictionary<string, SkillStatusEffectSpec>? skillEffects = null)
    {
        // Sum all unconsumed StartingGuardBonus modifiers (e.g. Éclat de garde: +8 garde).
        var guardBonus = runModifiers?
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => (int)m.Value) ?? 0;
        var activeModifiers = runModifiers ?? [];
        var attackPowerMultiplier = 1.0 + activeModifiers
            .Where(m => m.Type == RunModifierType.AttackPowerBonus && !m.IsConsumed)
            .Sum(m => m.Value);
        var activeClimate = ResolveActiveClimate(draft.RoomId, activeModifiers);

        if (activeClimate == RoomClimate.Rain)
        {
            guardBonus += 5;
        }

        // Item-driven emotional attack type for the player character (e.g. a mask
        // that turns the hero's attacks to Rupture). Null when no override is active.
        var attackTypeOverride = ResolveAttackTypeOverride(activeModifiers);

        var allies = draft.Allies
            .Select(ally =>
            {
                if (ally.IsProtagonist)
                {
                    // Protagonist: current HP/mana/charge from PlayerState, run stats,
                    // PlayerState skills, and the item-driven attack-type override.
                    var protagonistSkills = playerState?.Skills
                        .Select(s => CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            ScalePlayerSkillPower(s.EffectType, s.BasePower, attackPowerMultiplier),
                            statusEffect: EffectFor(skillEffects, s.Key)))
                        .ToArray()
                        ?? GetDefaultAllySkills(attackPowerMultiplier, skillEffects);

                    var maxVitality = playerState?.MaxVitality ?? 100;
                    var currentVitality = playerState?.CurrentVitality ?? maxVitality;
                    var guard = (playerState?.Guard ?? 0) + guardBonus;
                    var mana = playerState?.Mana ?? 0;
                    var charge = playerState?.Charge ?? 0;

                    var protagonist = Combatant.Create(
                        CombatantId.New(),
                        ally.AllyKey,
                        ally.DisplayName,
                        CombatantSide.Player,
                        ally.Role,
                        maxVitality,
                        currentVitality,
                        guard,
                        baseGuard: guardBonus,
                        mana,
                        charge,
                        protagonistSkills,
                        attackPower: attackPower,
                        defense: defense,
                        speed: speed,
                        focus: focus);

                    protagonist.ApplyAttackTypeOverride(attackTypeOverride);
                    return protagonist;
                }

                // Companion: its OWN kit and stats; starts the fight at full vitality.
                var companionSkills = ally.Skills is { Count: > 0 }
                    ? ally.Skills
                        .Select(s => CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            ScalePlayerSkillPower(s.EffectType, s.BasePower, attackPowerMultiplier),
                            statusEffect: EffectFor(skillEffects, s.Key)))
                        .ToArray()
                    : GetDefaultAllySkills(attackPowerMultiplier, skillEffects);

                var companionMaxVitality = ally.MaxVitality > 0 ? ally.MaxVitality : 100;

                var companion = Combatant.Create(
                    CombatantId.New(),
                    ally.AllyKey,
                    ally.DisplayName,
                    CombatantSide.Player,
                    ally.Role,
                    companionMaxVitality,
                    currentVitality: companionMaxVitality,
                    guard: ally.StartingGuard + guardBonus,
                    baseGuard: guardBonus,
                    mana: ally.Mana,
                    charge: ally.Charge,
                    companionSkills,
                    attackPower: ally.AttackPower,
                    defense: ally.Defense,
                    speed: ally.Speed,
                    focus: ally.Focus);

                // Companions keep their own emotional type (no item override).
                companion.ApplyAttackTypeOverride(null);
                return companion;
            })
            .ToArray();

        var (bossVitalityMultiplier, bossPowerMultiplier, bossGuardBonus) = EncounterBonus(draft.EncounterType);

        var enemies = draft.Enemies
            .Select(enemy =>
            {
                var baseVitality = (int)Math.Ceiling(
                    (EnemyVitalityBase + enemy.BaseDifficulty * VitalityPerDifficulty) * bossVitalityMultiplier); var representativePower = enemy.Skills.Count > 0
                    ? enemy.Skills.Max(s => s.BasePower)
                    : 0;

                var scaled = _enemyStatScaler.Scale(baseVitality, representativePower, draft.DifficultyMultiplier);
                var enemyPowerMultiplier = (activeClimate switch
                {
                    RoomClimate.Grey => 0.90,
                    RoomClimate.Heatwave => 1.10,
                    _ => 1.0
                }) * bossPowerMultiplier;
                var enemyStartingGuard = (palaceRoomState == PalaceRoomState.Silent ? 8 : 0) + bossGuardBonus;

                var skills = enemy.Skills
                    .Select(s =>
                    {
                        var scaledSkill = _enemyStatScaler.Scale(baseVitality, s.BasePower, draft.DifficultyMultiplier);
                        var power = ScaleEnemySkillPower(
                            s.EffectType,
                            scaledSkill.Power,
                            enemyPowerMultiplier,
                            palaceRoomState);
                        return CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            power,
                            s.Tags,
                            EffectFor(skillEffects, s.Key));
                    })
                    .ToArray();

                return Combatant.CreateEnemy(
                    sourceKey: enemy.EnemyKey,
                    displayName: enemy.DisplayName,
                    archetype: enemy.Archetype,
                    maxVitality: scaled.Vitality,
                    skills: skills,
                    startingGuard: enemyStartingGuard);
            })
            .ToArray();

        return Combat.Create(
            combatId,
            new RunId(draft.RunId),
            new RoomId(draft.RoomId),
            new NodeId(draft.NodeId),
            allies,
            enemies);
    }

    // Looks up the durable status a skill applies, by skill key (catalog-sourced).
    private static SkillStatusEffectSpec? EffectFor(
        IReadOnlyDictionary<string, SkillStatusEffectSpec>? effects,
        string key)
        => effects is not null && effects.TryGetValue(key, out var spec) ? spec : null;

    private static EmotionalType? ResolveAttackTypeOverride(IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(m => m.Type == RunModifierType.AttackTypeOverride && !m.IsConsumed)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();

        if (modifier is null)
        {
            return null;
        }

        var value = (int)Math.Round(modifier.Value);

        return Enum.IsDefined(typeof(EmotionalType), value) && value != (int)EmotionalType.Neutral
            ? (EmotionalType)value
            : null;
    }

    private static RoomClimate? ResolveActiveClimate(
        Guid roomId,
        IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == roomId)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => RoomClimate.Grey,
            2 => RoomClimate.Rain,
            3 => RoomClimate.Heatwave,
            4 => RoomClimate.Hail,
            _ => null
        };
    }

    private enum RoomClimate
    {
        Grey,
        Rain,
        Heatwave,
        Hail
    }

    private static string NormalizeCombatEffectType(string skillKey, string effectType)
    {
        if (string.Equals(skillKey, "skill.basic.guard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effectType, "AddCurrentGuard", StringComparison.OrdinalIgnoreCase))
        {
            return "Guard";
        }

        return effectType;
    }

    private static int ScalePlayerSkillPower(string effectType, int basePower, double attackPowerMultiplier)
    {
        if (!string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(effectType, "DamageVitality", StringComparison.OrdinalIgnoreCase))
        {
            return basePower;
        }

        return Math.Max(1, (int)Math.Round(basePower * attackPowerMultiplier));
    }

    private static int ScaleEnemySkillPower(
        string effectType,
        int basePower,
        double climateMultiplier,
        PalaceRoomState palaceRoomState)
    {
        var multiplier = climateMultiplier;
        if (palaceRoomState == PalaceRoomState.Painful && IsDamageEffect(effectType))
        {
            multiplier *= 0.90;
        }

        return Math.Max(1, (int)Math.Round(basePower * multiplier));
    }

    private static bool IsDamageEffect(string effectType)
    {
        return string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effectType, "DamageVitality", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<CombatantSkill> GetDefaultAllySkills(
        double attackPowerMultiplier,
        IReadOnlyDictionary<string, SkillStatusEffectSpec>? skillEffects)
    {
        return
        [
            CombatantSkill.Create(
                key: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingType: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: ScalePlayerSkillPower("Damage", 10, attackPowerMultiplier),
                statusEffect: EffectFor(skillEffects, "skill.basic.strike")),
            CombatantSkill.Create(
                key: "skill.basic.guard",
                displayName: "Garde",
                skillType: "Defense",
                targetingType: "Self",
                effectType: "Guard",
                manaCost: 0,
                chargeCost: 0,
                basePower: 5,
                statusEffect: EffectFor(skillEffects, "skill.basic.guard"))
        ];
    }
}