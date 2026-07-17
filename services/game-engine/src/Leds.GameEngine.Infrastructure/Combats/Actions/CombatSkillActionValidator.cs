using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Targeting;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Combats.Actions;

public sealed class CombatSkillActionValidator : ICombatSkillActionValidator
{
    private readonly ICombatTargetingRuleValidator _targetingRuleValidator;

    public CombatSkillActionValidator(ICombatTargetingRuleValidator targetingRuleValidator)
    {
        _targetingRuleValidator = targetingRuleValidator;
    }

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

        // "Loi de l'Éloge Funèbre": after any death, the next actor may only use the
        // basic attack. Documented simplification: item use and Reposition never call
        // through this shared validator, so neither is gated by this restriction.
        if (combat.NextActionRestrictedToBasicAttack
            && !string.Equals(skill.Key, BasicAttackSkillKey, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Loi de l'Éloge Funèbre: seule une attaque basique est permise après une mort.");
        }

        var targetingResult = _targetingRuleValidator.Validate(combat, actor, skill, targetIds);

        if (!targetingResult.IsValid)
        {
            return Invalid(targetingResult.ErrorMessage!);
        }

        // A valid action (necessarily the basic attack, if the restriction was active)
        // consumes the restriction so it doesn't linger into the actor after next.
        combat.ConsumeBasicAttackRestriction();

        return new CombatSkillActionValidationResult(
            IsValid: true,
            ErrorMessage: null,
            Actor: actor,
            Skill: skill,
            Targets: targetingResult.Targets);
    }

    private const string BasicAttackSkillKey = "skill.basic.strike";

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