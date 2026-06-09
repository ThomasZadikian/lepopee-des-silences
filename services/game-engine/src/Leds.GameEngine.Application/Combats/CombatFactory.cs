using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats;

public sealed class CombatFactory : ICombatFactory
{
    private const int DefaultAllyVitality = 100;
    private const int EnemyVitalityBase = 40;
    private const int VitalityPerDifficulty = 10;

    public Combat CreateFromDraft(CombatEncounterDraft draft)
    {
        var allies = draft.Allies
            .Select(ally => Combatant.CreateAlly(
                sourceKey: ally.AllyKey,
                displayName: ally.DisplayName,
                archetype: ally.Role,
                maxVitality: DefaultAllyVitality))
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
}
