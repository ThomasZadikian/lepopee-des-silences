using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Typing;

public sealed record EmotionalAffinityRuleSnapshot(
    EmotionalType AttackingRegister,
    EmotionalType DefendingRegister,
    DamageEffectiveness Effectiveness,
    double Multiplier);

/// <summary>
/// Immutable copy of the global emotional matrix used by a run. Keeping the complete
/// matrix with the run makes combat deterministic even after Catalog publishes a new version.
/// </summary>
public sealed class EmotionalAffinityMatrixSnapshot
{
    private readonly IReadOnlyDictionary<(EmotionalType Attack, EmotionalType Defense), EmotionalAffinityRuleSnapshot> _rules;

    private EmotionalAffinityMatrixSnapshot(
        string version,
        IReadOnlyDictionary<(EmotionalType Attack, EmotionalType Defense), EmotionalAffinityRuleSnapshot> rules)
    {
        Version = version;
        _rules = rules;
    }

    public string Version { get; }

    public IReadOnlyCollection<EmotionalAffinityRuleSnapshot> Rules => _rules
        .Select(rule => rule.Value)
        .ToArray();

    public static EmotionalAffinityMatrixSnapshot Create(
        string version,
        IEnumerable<EmotionalAffinityRuleSnapshot> rules)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("Emotional affinity matrix version is required.");

        ArgumentNullException.ThrowIfNull(rules);
        var materialized = rules.ToArray();
        var registers = Enum.GetValues<EmotionalType>();
        var expectedRuleCount = registers.Length * registers.Length;

        if (materialized.Length != expectedRuleCount)
            throw new DomainException(
                $"Emotional affinity matrix '{version}' must contain exactly {expectedRuleCount} rules.");

        var dictionary = new Dictionary<(EmotionalType, EmotionalType), EmotionalAffinityRuleSnapshot>();
        foreach (var rule in materialized)
        {
            if (!double.IsFinite(rule.Multiplier) || rule.Multiplier < 0)
                throw new DomainException(
                    $"Emotional affinity matrix '{version}' contains an invalid multiplier.");

            if (!dictionary.TryAdd((rule.AttackingRegister, rule.DefendingRegister), rule))
                throw new DomainException(
                    $"Emotional affinity matrix '{version}' contains duplicate rule " +
                    $"{rule.AttackingRegister}->{rule.DefendingRegister}.");
        }

        foreach (var attack in registers)
        foreach (var defense in registers)
        {
            if (!dictionary.ContainsKey((attack, defense)))
                throw new DomainException(
                    $"Emotional affinity matrix '{version}' is missing rule {attack}->{defense}.");
        }

        return new EmotionalAffinityMatrixSnapshot(version.Trim(), dictionary);
    }

    public DamageEffectiveness Resolve(EmotionalType attack, EmotionalType defense) =>
        _rules.TryGetValue((attack, defense), out var rule)
            ? rule.Effectiveness
            : throw new DomainException(
                $"Emotional affinity matrix '{Version}' has no rule for {attack}->{defense}.");

    public double ResolveMultiplier(EmotionalType attack, EmotionalType defense) =>
        _rules.TryGetValue((attack, defense), out var rule)
            ? rule.Multiplier
            : throw new DomainException(
                $"Emotional affinity matrix '{Version}' has no multiplier for {attack}->{defense}.");

}
