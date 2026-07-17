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
            "SingleEnemy" => ValidateSingleEnemy(actor, skill, targets),
            "SingleAlly" => ValidateSingleAlly(actor, skill, targets),
            "AllEnemies" => ValidateAllEnemies(combat, actor, skill, targets),
            "AllAllies" => ValidateAllAllies(combat, actor, skill, targets),
            _ => Invalid($"Unsupported targeting type: {skill.TargetingType}")
        };
    }

    /// <summary>
    /// Physical-category ("short range") skills cannot reach a Back-row combatant at
    /// all — the row's "hors de portée" untargetable rule. Magic-category skills are
    /// unaffected. Applied uniformly to enemy and ally targeting: the rule is about
    /// physical reach, not specifically about attacking enemies.
    /// </summary>
    private static bool IsOutOfPhysicalRange(CombatantSkill skill, Combatant target) =>
        string.Equals(skill.Category, "Physical", StringComparison.OrdinalIgnoreCase)
        && target.Row == CombatRow.Back;

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
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count != 1)
        {
            return Invalid("SingleEnemy targeting requires exactly one target.");
        }

        var target = targets.Single();

        if (target.Side == actor.Side)
        {
            return Invalid("SingleEnemy targeting requires a target from the opposite side.");
        }

        if (IsOutOfPhysicalRange(skill, target))
        {
            return Invalid("Physical-category skills cannot target a Back row combatant.");
        }

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateSingleAlly(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count != 1)
        {
            return Invalid("SingleAlly targeting requires exactly one target.");
        }

        var target = targets.Single();

        if (target.Side != actor.Side)
        {
            return Invalid("SingleAlly targeting requires a target from the same side.");
        }

        if (IsOutOfPhysicalRange(skill, target))
        {
            return Invalid("Physical-category skills cannot target a Back row combatant.");
        }

        return Valid(targets);
    }

    private static CombatTargetingValidationResult ValidateAllEnemies(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
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
            .Where(c => c.Side != actor.Side && !c.IsDefeated && !IsOutOfPhysicalRange(skill, c))
            .ToArray();

        return ValidateExplicitTargetSet(targets, expectedTargets, "AllEnemies targeting requires all active, reachable enemies.");
    }

    private static CombatTargetingValidationResult ValidateAllAllies(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
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
            .Where(c => c.Side == actor.Side && !c.IsDefeated && !IsOutOfPhysicalRange(skill, c))
            .ToArray();

        return ValidateExplicitTargetSet(targets, expectedTargets, "AllAllies targeting requires all active, reachable allies.");
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