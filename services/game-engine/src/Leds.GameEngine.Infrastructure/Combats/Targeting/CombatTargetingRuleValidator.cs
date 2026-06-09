using Leds.GameEngine.Application.Combats.Targeting;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Combats.Targeting;

public sealed class CombatTargetingRuleValidator : ICombatTargetingRuleValidator
{
    public CombatTargetingValidationResult Validate(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Guid> targetIds)
    {
        var targetResolution = ResolveTargets(combat, targetIds);

        if (!targetResolution.IsValid)
        {
            return targetResolution;
        }

        var targets = targetResolution.Targets;

        return skill.TargetingType switch
        {
            "Self" => ValidateSelf(actor, targets),
            "SingleEnemy" => ValidateSingleEnemy(actor, targets),
            "SingleAlly" => ValidateSingleAlly(actor, targets),
            "AllEnemies" => ValidateAllEnemies(combat, actor, targets),
            "AllAllies" => ValidateAllAllies(combat, actor, targets),
            _ => Invalid($"Unsupported targeting type: {skill.TargetingType}")
        };
    }

    private static CombatTargetingValidationResult ResolveTargets(
        Combat combat,
        IReadOnlyCollection<Guid> targetIds)
    {
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

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateSelf(
        Combatant actor,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count != 1)
        {
            return Invalid("Self targeting requires exactly one target.");
        }

        if (targets.Single().Id != actor.Id)
        {
            return Invalid("Self targeting requires the actor to target themselves.");
        }

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateSingleEnemy(
        Combatant actor,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count != 1)
        {
            return Invalid("SingleEnemy targeting requires exactly one target.");
        }

        if (targets.Single().Side == actor.Side)
        {
            return Invalid("SingleEnemy targeting requires a target from the opposite side.");
        }

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateSingleAlly(
        Combatant actor,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count != 1)
        {
            return Invalid("SingleAlly targeting requires exactly one target.");
        }

        if (targets.Single().Side != actor.Side)
        {
            return Invalid("SingleAlly targeting requires a target from the same side.");
        }

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateAllEnemies(
        Combat combat,
        Combatant actor,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count == 0)
        {
            return Invalid("AllEnemies targeting requires explicit targets.");
        }

        if (targets.Any(t => t.Side == actor.Side))
        {
            return Invalid("AllEnemies targeting requires all targets to be from the opposite side.");
        }

        var expectedTargets = GetAllCombatants(combat)
            .Where(c => c.Side != actor.Side && !c.IsDefeated)
            .ToArray();

        return ValidateExplicitTargetSet(targets, expectedTargets, "AllEnemies targeting requires all active enemies.");
    }

    private static CombatTargetingValidationResult ValidateAllAllies(
        Combat combat,
        Combatant actor,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count == 0)
        {
            return Invalid("AllAllies targeting requires explicit targets.");
        }

        if (targets.Any(t => t.Side != actor.Side))
        {
            return Invalid("AllAllies targeting requires all targets to be from the same side.");
        }

        var expectedTargets = GetAllCombatants(combat)
            .Where(c => c.Side == actor.Side && !c.IsDefeated)
            .ToArray();

        return ValidateExplicitTargetSet(targets, expectedTargets, "AllAllies targeting requires all active allies.");
    }

    private static CombatTargetingValidationResult ValidateExplicitTargetSet(
        IReadOnlyCollection<Combatant> targets,
        IReadOnlyCollection<Combatant> expectedTargets,
        string errorMessage)
    {
        var targetIds = targets.Select(t => t.Id).Distinct().ToArray();
        var expectedIds = expectedTargets.Select(t => t.Id).ToArray();

        if (targetIds.Length != expectedIds.Length || expectedIds.Any(id => !targetIds.Contains(id)))
        {
            return Invalid(errorMessage);
        }

        return Valid(targets);
    }

    private static IReadOnlyCollection<Combatant> GetAllCombatants(Combat combat)
    {
        var list = new List<Combatant>(combat.Allies.Count + combat.Enemies.Count);
        list.AddRange(combat.Allies);
        list.AddRange(combat.Enemies);
        return list;
    }

    private static CombatTargetingValidationResult Valid(IReadOnlyCollection<Combatant> targets)
    {
        return new CombatTargetingValidationResult(true, null, targets);
    }

    private static CombatTargetingValidationResult Invalid(string message)
    {
        return new CombatTargetingValidationResult(false, message, []);
    }
}
