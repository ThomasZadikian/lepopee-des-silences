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

    public Combat CreateFromDraft(CombatEncounterDraft draft, PlayerRuntimeState? playerState = null)
    {
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
                var guard = playerState?.Guard ?? 0;
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
                    mana,
                    charge,
                    skills);
            })
            .ToArray();

        var enemies = draft.Enemies
            .Select(enemy =>
            {
                var maxVitality = EnemyVitalityBase + enemy.BaseDifficulty * VitalityPerDifficulty;

                var skills = enemy.Skills
                    .Select(s => CombatantSkill.Create(
                        s.Key,
                        s.DisplayName,
                        s.SkillType,
                        s.TargetingType,
                        s.EffectType,
                        s.ManaCost,
                        s.ChargeCost,
                        s.BasePower,
                        s.Tags))
                    .ToArray();

                return Combatant.CreateEnemy(
                    sourceKey: enemy.EnemyKey,
                    displayName: enemy.DisplayName,
                    archetype: enemy.Archetype,
                    maxVitality: maxVitality,
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
