using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Domain.Gameplay;

public enum AffinityOutcome
{
    Neutral = 0,
    Weak = 1,
    Resistant = 2,
    Immune = 3
}

public sealed record EmotionalAffinityRule(
    EmotionalRegister AttackingRegister,
    EmotionalRegister DefendingRegister,
    AffinityOutcome Outcome);

/// <summary>
/// Complete, immutable affinity matrix. Construction fails unless every active
/// attacking/defending pair is declared exactly once.
/// </summary>
public sealed class EmotionalAffinityMatrix
{
    public const string CanonicalVersion = "emotional-affinity-1.0.0";

    private readonly IReadOnlyDictionary<(EmotionalRegister Attack, EmotionalRegister Defense), AffinityOutcome> _rules;

    private EmotionalAffinityMatrix(
        string version,
        IReadOnlyDictionary<(EmotionalRegister Attack, EmotionalRegister Defense), AffinityOutcome> rules)
    {
        Version = version;
        _rules = rules;
    }

    public string Version { get; }

    /// <summary>
    /// Canonical Catalog-owned matrix. Neutral has no intrinsic interaction;
    /// every authored register has exactly one weakness, resistance and immunity.
    /// </summary>
    public static EmotionalAffinityMatrix Canonical { get; } = CreateCanonical();

    public IReadOnlyCollection<EmotionalAffinityRule> Rules => _rules
        .Select(rule => new EmotionalAffinityRule(rule.Key.Attack, rule.Key.Defense, rule.Value))
        .ToArray();

    public static EmotionalAffinityMatrix Create(
        string version,
        IEnumerable<EmotionalAffinityRule> rules)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("Emotional affinity matrix version is required.");

        ArgumentNullException.ThrowIfNull(rules);

        var activeRegisters = EmotionalRegisterCatalog.Active.Select(d => d.Value).ToHashSet();
        var materialized = rules.ToArray();

        var unknown = materialized.FirstOrDefault(rule =>
            !activeRegisters.Contains(rule.AttackingRegister)
            || !activeRegisters.Contains(rule.DefendingRegister));

        if (unknown is not null)
            throw new DomainException("Emotional affinity matrix references an inactive or unknown register.");

        var duplicates = materialized
            .GroupBy(rule => (rule.AttackingRegister, rule.DefendingRegister))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.AttackingRegister}->{group.Key.DefendingRegister}")
            .ToArray();

        if (duplicates.Length > 0)
            throw new DomainException($"Emotional affinity matrix contains duplicate pairs: {string.Join(", ", duplicates)}.");

        var expectedCount = activeRegisters.Count * activeRegisters.Count;
        if (materialized.Length != expectedCount)
        {
            var provided = materialized
                .Select(rule => (rule.AttackingRegister, rule.DefendingRegister))
                .ToHashSet();
            var missing = activeRegisters
                .SelectMany(attack => activeRegisters.Select(defense => (attack, defense)))
                .Where(pair => !provided.Contains(pair))
                .Select(pair => $"{pair.attack}->{pair.defense}")
                .ToArray();

            throw new DomainException(
                $"Emotional affinity matrix must contain exactly {expectedCount} rules. " +
                $"Missing pairs: {string.Join(", ", missing)}.");
        }

        return new EmotionalAffinityMatrix(
            version.Trim(),
            materialized.ToDictionary(
                rule => (rule.AttackingRegister, rule.DefendingRegister),
                rule => rule.Outcome));
    }

    public AffinityOutcome Resolve(EmotionalRegister attack, EmotionalRegister defense)
    {
        if (!_rules.TryGetValue((attack, defense), out var outcome))
            throw new DomainException($"No affinity rule exists for {attack}->{defense}.");

        return outcome;
    }

    private static EmotionalAffinityMatrix CreateCanonical()
    {
        var rules = EmotionalRegisterCatalog.Active
            .SelectMany(attack => EmotionalRegisterCatalog.Active.Select(defense =>
                new EmotionalAffinityRule(
                    attack.Value,
                    defense.Value,
                    ResolveCanonicalOutcome(attack.Value, defense.Value))))
            .ToArray();

        return Create(CanonicalVersion, rules);
    }

    private static AffinityOutcome ResolveCanonicalOutcome(
        EmotionalRegister attack,
        EmotionalRegister defense)
    {
        if (attack == EmotionalRegister.Neutral || defense == EmotionalRegister.Neutral)
            return AffinityOutcome.Neutral;

        var (weak, resistant, immune) = defense switch
        {
            EmotionalRegister.Effroi => (EmotionalRegister.Memoire, EmotionalRegister.Rupture, EmotionalRegister.Silence),
            EmotionalRegister.Deni => (EmotionalRegister.Melancolie, EmotionalRegister.Effroi, EmotionalRegister.Folie),
            EmotionalRegister.Melancolie => (EmotionalRegister.Silence, EmotionalRegister.Memoire, EmotionalRegister.Effroi),
            EmotionalRegister.Rupture => (EmotionalRegister.Folie, EmotionalRegister.Melancolie, EmotionalRegister.Deni),
            EmotionalRegister.Memoire => (EmotionalRegister.Deni, EmotionalRegister.Folie, EmotionalRegister.Rupture),
            EmotionalRegister.Silence => (EmotionalRegister.Rupture, EmotionalRegister.Deni, EmotionalRegister.Memoire),
            EmotionalRegister.Folie => (EmotionalRegister.Effroi, EmotionalRegister.Silence, EmotionalRegister.Melancolie),
            _ => throw new DomainException($"No canonical affinity profile exists for defending register '{defense}'.")
        };

        if (attack == weak) return AffinityOutcome.Weak;
        if (attack == resistant) return AffinityOutcome.Resistant;
        if (attack == immune) return AffinityOutcome.Immune;
        return AffinityOutcome.Neutral;
    }
}
