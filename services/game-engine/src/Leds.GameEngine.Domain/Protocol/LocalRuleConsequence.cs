using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// One rung of a <see cref="LocalRule"/>'s graduated consequence ladder: unlocks once the rule's
/// cumulative severity reaches <see cref="SeverityThreshold"/> (see
/// <see cref="LocalRuleState.RegisterTransgression"/>). Consequences below Combat are meant to
/// stack — reaching threshold 3 also re-applies whatever thresholds 1 and 2 dictated — so a rule
/// escalates rather than jumping straight to its worst outcome.
/// </summary>
public sealed record LocalRuleConsequence
{
    private LocalRuleConsequence(
        int severityThreshold,
        LocalRuleConsequenceType type,
        string? targetNpcCatalogKey)
    {
        SeverityThreshold = severityThreshold;
        Type = type;
        TargetNpcCatalogKey = targetNpcCatalogKey;
    }

    public int SeverityThreshold { get; }

    public LocalRuleConsequenceType Type { get; }

    /// <summary>Which present NPC reacts — required for NpcRelocate/AttitudeChange/
    /// IncreasedSurveillance, meaningless for the others.</summary>
    public string? TargetNpcCatalogKey { get; }

    public static LocalRuleConsequence Create(
        int severityThreshold,
        LocalRuleConsequenceType type,
        string? targetNpcCatalogKey = null)
    {
        if (severityThreshold < 1)
        {
            throw new DomainException("A local rule consequence's severity threshold must be at least 1.");
        }

        var needsTarget = type is LocalRuleConsequenceType.NpcRelocate
            or LocalRuleConsequenceType.AttitudeChange
            or LocalRuleConsequenceType.IncreasedSurveillance;

        if (needsTarget && string.IsNullOrWhiteSpace(targetNpcCatalogKey))
        {
            throw new DomainException($"A {type} consequence requires a target NPC catalog key.");
        }

        return new LocalRuleConsequence(
            severityThreshold, type, targetNpcCatalogKey?.Trim());
    }
}
