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
        IReadOnlyCollection<RunModifier>? runModifiers = null)
    {
        // Sum all unconsumed StartingGuardBonus modifiers (e.g. Éclat de garde: +8 garde).
        var guardBonus = runModifiers?
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => (int)m.Value) ?? 0;

        var allies = draft.Allies
            .Select(ally =>
            {
                var skills = playerState?.Skills
                    .Select(s => CombatantSkill.Create(
                        s.Key,
                        s.DisplayName,
                        s.SkillType,
                        s.TargetingType,
                        s.EffectType,
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
                    baseGuard: guardBonus,  // passive floor restored at round start
                    mana,
                    charge,
                    skills);
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

                var skills = enemy.Skills
                    .Select(s =>
                    {
                        var scaledSkill = _enemyStatScaler.Scale(baseVitality, s.BasePower, draft.DifficultyMultiplier);
                        return CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            s.EffectType,
                            s.ManaCost,
                            s.ChargeCost,
                            scaledSkill.Power,
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
            CombatId.New(),
            new RunId(draft.RunId),
            new RoomId(draft.RoomId),
            new NodeId(draft.NodeId),
            allies,
            enemies);
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