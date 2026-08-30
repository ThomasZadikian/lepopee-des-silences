using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// Run-scoped progress against one <see cref="LocalRule"/> instance — joined to it only by
/// <see cref="LocalRuleKey"/>, never a hard reference, same convention as
/// <see cref="Npcs.NpcRelationship"/>'s join to its catalog NPC. Cumulative severity only ever
/// rises within a run; nothing in this engine de-escalates it.
/// </summary>
public sealed class LocalRuleState
{
    private readonly HashSet<int> _triggeredThresholds;

    private LocalRuleState(
        string localRuleKey,
        int cumulativeSeverity,
        bool hasBeenInformed,
        HashSet<int> triggeredThresholds)
    {
        LocalRuleKey = localRuleKey;
        CumulativeSeverity = cumulativeSeverity;
        HasBeenInformed = hasBeenInformed;
        _triggeredThresholds = triggeredThresholds;
    }

    public string LocalRuleKey { get; }

    public int CumulativeSeverity { get; private set; }

    public bool HasBeenInformed { get; private set; }

    /// <summary>Consequence thresholds already returned by a prior
    /// <see cref="RegisterTransgression"/> call, so escalating further never repeats one.</summary>
    public IReadOnlyCollection<int> TriggeredThresholds => _triggeredThresholds;

    public static LocalRuleState Create(string localRuleKey)
    {
        if (string.IsNullOrWhiteSpace(localRuleKey))
        {
            throw new DomainException("Local rule state requires a local rule key.");
        }

        return new LocalRuleState(localRuleKey.Trim(), cumulativeSeverity: 0, hasBeenInformed: false, []);
    }

    public static LocalRuleState Rehydrate(
        string localRuleKey,
        int cumulativeSeverity,
        bool hasBeenInformed,
        IReadOnlyCollection<int> triggeredThresholds)
    {
        return new LocalRuleState(
            localRuleKey, cumulativeSeverity, hasBeenInformed, new HashSet<int>(triggeredThresholds));
    }

    /// <summary>First contact with the rule's condition: informs without accumulating severity.
    /// A no-op past the first call — being informed again isn't itself a transgression.</summary>
    public void MarkInformed() => HasBeenInformed = true;

    /// <summary>
    /// Registers one transgression against <paramref name="rule"/> and returns whichever
    /// consequences newly cross their threshold — never a consequence already returned by an
    /// earlier call, even if this call crosses more than one threshold at once.
    /// </summary>
    public IReadOnlyList<LocalRuleConsequence> RegisterTransgression(LocalRule rule, int severityIncrement = 1)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.Key != LocalRuleKey)
        {
            throw new DomainException("Local rule state does not belong to this local rule.");
        }

        if (severityIncrement < 1)
        {
            throw new DomainException("A transgression must increase severity by at least 1.");
        }

        CumulativeSeverity += severityIncrement;

        var newlyTriggered = rule.Consequences
            .Where(c => c.SeverityThreshold <= CumulativeSeverity && !_triggeredThresholds.Contains(c.SeverityThreshold))
            .ToList();

        foreach (var consequence in newlyTriggered)
        {
            _triggeredThresholds.Add(consequence.SeverityThreshold);
        }

        return newlyTriggered;
    }
}
