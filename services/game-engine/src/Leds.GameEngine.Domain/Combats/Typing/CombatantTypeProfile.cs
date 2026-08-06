namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// A combatant's emotional type identity: the type its attacks deal by default,
/// plus the fully resolved incoming profile from the run's Catalog snapshot.
/// All effectiveness and multiplier values are resolved from the run's Catalog snapshot.
/// </summary>
public sealed class CombatantTypeProfile
{
    public CombatantTypeProfile(
        EmotionalType attackType,
        IReadOnlyDictionary<EmotionalType, BaseEmotionalAffinity> baseAffinities,
        IReadOnlyCollection<EmotionalAffinityModifier>? modifiers = null)
    {
        AttackType = attackType;
        BaseAffinities = baseAffinities;
        Modifiers = modifiers ?? [];
    }

    /// <summary>The type this combatant's damaging skills deal when a skill declares none itself.</summary>
    public EmotionalType AttackType { get; }

    public IReadOnlyDictionary<EmotionalType, BaseEmotionalAffinity> BaseAffinities { get; }
    public IReadOnlyCollection<EmotionalAffinityModifier> Modifiers { get; }

    /// <summary>
    /// Resolves how effective an incoming attack type is against this profile.
    /// Local overrides win by explicit priority; otherwise the Catalog snapshot decides.
    /// </summary>
    public DamageEffectiveness EffectivenessAgainst(EmotionalType incoming)
    {
        var overriding = Modifiers
            .Where(modifier => !modifier.IsExpired
                && modifier.IncomingRegister == incoming
                && modifier.OutcomeOverride is not null)
            .OrderByDescending(modifier => modifier.Priority)
            .ThenBy(modifier => modifier.SourceKey, StringComparer.Ordinal)
            .FirstOrDefault();
        if (overriding?.OutcomeOverride is { } outcome)
            return outcome;

        return BaseAffinityAgainst(incoming).Outcome;
    }

    public int MultiplierPercentAgainst(EmotionalType incoming) => Modifiers
        .Where(modifier => !modifier.IsExpired && modifier.IncomingRegister == incoming)
        .Sum(modifier => modifier.MultiplierPercent);

    public double BaseMultiplierAgainst(EmotionalType incoming) => BaseAffinityAgainst(incoming).Multiplier;

    private BaseEmotionalAffinity BaseAffinityAgainst(EmotionalType incoming) =>
        BaseAffinities.TryGetValue(incoming, out var affinity)
            ? affinity
            : throw new InvalidOperationException(
                $"Catalog affinity profile has no rule for incoming register '{incoming}'.");
}

public sealed record BaseEmotionalAffinity(DamageEffectiveness Outcome, double Multiplier);
