using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats;

public sealed class CombatFactory : ICombatFactory
{
    private const int EnemyVitalityBase = 40;
    private const int VitalityPerDifficulty = 10;

    private readonly EnemyStatScaler _enemyStatScaler = new();

    public Combat CreateFromDraft(
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10)
    {
        return CreateFromDraft(
            CombatId.New(),
            draft,
            playerState,
            runModifiers,
            attackPower,
            defense,
            speed);
    }

    public Combat CreateFromDraft(
        CombatId combatId,
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10)
    {
        // Sum all unconsumed StartingGuardBonus modifiers (e.g. Éclat de garde: +8 garde).
        var guardBonus = runModifiers?
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => (int)m.Value) ?? 0;
        var activeClimate = ResolveActiveClimate(draft.RoomId, runModifiers ?? []);

        if (activeClimate == RoomClimate.Rain)
        {
            guardBonus += 5;
        }

        var allies = draft.Allies
            .Select(ally =>
            {
                var skills = playerState?.Skills
                    .Select(s => CombatantSkill.Create(
                        s.Key,
                        s.DisplayName,
                        s.SkillType,
                        s.TargetingType,
                        NormalizeCombatEffectType(s.Key, s.EffectType),
                        s.ManaCost,
                        s.ChargeCost,
                        s.BasePower))
                    .ToArray()
                    ?? GetDefaultAllySkills();

                var maxVitality = playerState?.MaxVitality ?? 100;
                var currentVitality = playerState?.CurrentVitality ?? maxVitality;
                var guard = (playerState?.Guard ?? 0) + guardBonus;
                var mana = playerState?.Mana ?? 0;
                var charge = playerState?.Charge ?? 0;

                return Combatant.Create(
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
                    skills,
                    attackPower: attackPower,
                    defense: defense,
                    speed: speed);
            })
            .ToArray();

        var enemies = draft.Enemies
            .Select(enemy =>
            {
                var baseVitality = EnemyVitalityBase + enemy.BaseDifficulty * VitalityPerDifficulty;
                var representativePower = enemy.Skills.Count > 0
                    ? enemy.Skills.Max(s => s.BasePower)
                    : 0;

                var scaled = _enemyStatScaler.Scale(baseVitality, representativePower, draft.DifficultyMultiplier);
                var enemyPowerMultiplier = activeClimate switch
                {
                    RoomClimate.Grey => 0.90,
                    RoomClimate.Heatwave => 1.10,
                    _ => 1.0
                };

                var skills = enemy.Skills
                    .Select(s =>
                    {
                        var scaledSkill = _enemyStatScaler.Scale(baseVitality, s.BasePower, draft.DifficultyMultiplier);
                        var power = Math.Max(1, (int)Math.Round(scaledSkill.Power * enemyPowerMultiplier));
                        return CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            power,
                            s.Tags);
                    })
                    .ToArray();

                return Combatant.CreateEnemy(
                    sourceKey: enemy.EnemyKey,
                    displayName: enemy.DisplayName,
                    archetype: enemy.Archetype,
                    maxVitality: scaled.Vitality,
                    skills: skills);
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

    private static IReadOnlyCollection<CombatantSkill> GetDefaultAllySkills()
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
                basePower: 10),
            CombatantSkill.Create(
                key: "skill.basic.guard",
                displayName: "Garde",
                skillType: "Defense",
                targetingType: "Self",
                effectType: "Guard",
                manaCost: 0,
                chargeCost: 0,
                basePower: 5)
        ];
    }
}
