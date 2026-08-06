using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Typing;

public sealed record EmotionalAffinityRuleSnapshot(
    EmotionalType AttackingRegister,
    EmotionalType DefendingRegister,
    DamageEffectiveness Effectiveness);

/// <summary>
/// Immutable copy of the global emotional matrix used by a run. Keeping the complete
/// matrix with the run makes combat deterministic even after Catalog publishes a new version.
/// </summary>
public sealed class EmotionalAffinityMatrixSnapshot
{
    private readonly IReadOnlyDictionary<(EmotionalType Attack, EmotionalType Defense), DamageEffectiveness> _rules;

    private EmotionalAffinityMatrixSnapshot(
        string version,
        IReadOnlyDictionary<(EmotionalType Attack, EmotionalType Defense), DamageEffectiveness> rules)
    {
        Version = version;
        _rules = rules;
    }

    public string Version { get; }

    public IReadOnlyCollection<EmotionalAffinityRuleSnapshot> Rules => _rules
        .Select(rule => new EmotionalAffinityRuleSnapshot(rule.Key.Attack, rule.Key.Defense, rule.Value))
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

        var dictionary = new Dictionary<(EmotionalType, EmotionalType), DamageEffectiveness>();
        foreach (var rule in materialized)
        {
            if (!dictionary.TryAdd((rule.AttackingRegister, rule.DefendingRegister), rule.Effectiveness))
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
        _rules.TryGetValue((attack, defense), out var effectiveness)
            ? effectiveness
            : throw new DomainException(
                $"Emotional affinity matrix '{Version}' has no rule for {attack}->{defense}.");

    /// <summary>Canonical fixture for direct domain construction; application flows snapshot Catalog.</summary>
    public static EmotionalAffinityMatrixSnapshot Canonical { get; } = CreateCanonical();

    private static EmotionalAffinityMatrixSnapshot CreateCanonical()
    {
        var registers = Enum.GetValues<EmotionalType>();
        var rules = from attack in registers
                    from defense in registers
                    select new EmotionalAffinityRuleSnapshot(
                        attack,
                        defense,
                        ResolveCanonical(attack, defense));
        return Create("emotional-affinity-1.0.0", rules);
    }

    private static DamageEffectiveness ResolveCanonical(EmotionalType attack, EmotionalType defense)
    {
        if (attack == EmotionalType.Neutral || defense == EmotionalType.Neutral)
            return DamageEffectiveness.Neutral;

        var (weak, resistant, immune) = defense switch
        {
            EmotionalType.Effroi => (EmotionalType.Memoire, EmotionalType.Rupture, EmotionalType.Silence),
            EmotionalType.Deni => (EmotionalType.Melancolie, EmotionalType.Effroi, EmotionalType.Folie),
            EmotionalType.Melancolie => (EmotionalType.Silence, EmotionalType.Memoire, EmotionalType.Effroi),
            EmotionalType.Rupture => (EmotionalType.Folie, EmotionalType.Melancolie, EmotionalType.Deni),
            EmotionalType.Memoire => (EmotionalType.Deni, EmotionalType.Folie, EmotionalType.Rupture),
            EmotionalType.Silence => (EmotionalType.Rupture, EmotionalType.Deni, EmotionalType.Memoire),
            EmotionalType.Folie => (EmotionalType.Effroi, EmotionalType.Silence, EmotionalType.Melancolie),
            _ => (EmotionalType.Neutral, EmotionalType.Neutral, EmotionalType.Neutral)
        };

        if (attack == immune) return DamageEffectiveness.Immune;
        if (attack == weak) return DamageEffectiveness.Weak;
        if (attack == resistant) return DamageEffectiveness.Resistant;
        return DamageEffectiveness.Neutral;
    }
}
