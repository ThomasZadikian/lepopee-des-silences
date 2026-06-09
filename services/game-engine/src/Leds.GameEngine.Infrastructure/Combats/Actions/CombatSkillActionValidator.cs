using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Combats.Actions;

public sealed class CombatSkillActionValidator : ICombatSkillActionValidator
{
    public CombatSkillActionValidationResult Validate(
        Combat combat,
        Guid actorId,
        string skillKey,
        IReadOnlyCollection<Guid> targetIds)
    {
        if (combat.Status != CombatStatus.Active)
        {
            return Invalid("Combat is not active.");
        }

        var actor = GetAllCombatants(combat)
            .FirstOrDefault(c => c.Id.Value == actorId);

        if (actor is null)
        {
            return Invalid("Actor does not exist in this combat.");
        }

        if (actor.IsDefeated)
        {
            return Invalid("Actor is defeated.");
        }

        var skill = actor.Skills
            .FirstOrDefault(s => string.Equals(s.Key, skillKey, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
        {
            return Invalid($"Actor does not own skill '{skillKey}'.");
        }

        if (targetIds.Count == 0)
        {
            return Invalid("At least one target is required.");
        }

        var allCombatants = GetAllCombatants(combat);
        var targets = new List<Combatant>();

        foreach (var targetId in targetIds)
        {
            var target = allCombatants.FirstOrDefault(c => c.Id.Value == targetId);

            if (target is null)
            {
                return Invalid($"Target with id '{targetId}' does not exist in this combat.");
            }

            if (target.IsDefeated)
            {
                return Invalid($"Target with id '{targetId}' is defeated.");
            }

            targets.Add(target);
        }

        return new CombatSkillActionValidationResult(
            IsValid: true,
            ErrorMessage: null,
            Actor: actor,
            Skill: skill,
            Targets: targets);
    }

    private static CombatSkillActionValidationResult Invalid(string message)
    {
        return new CombatSkillActionValidationResult(
            IsValid: false,
            ErrorMessage: message,
            Actor: null,
            Skill: null,
            Targets: []);
    }

    private static IReadOnlyCollection<Combatant> GetAllCombatants(Combat combat)
    {
        var list = new List<Combatant>(combat.Allies.Count + combat.Enemies.Count);
        list.AddRange(combat.Allies);
        list.AddRange(combat.Enemies);
        return list;
    }
}
