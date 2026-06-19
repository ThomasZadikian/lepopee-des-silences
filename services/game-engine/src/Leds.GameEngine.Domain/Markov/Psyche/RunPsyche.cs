namespace Leds.GameEngine.Domain.Markov.Psyche;

/// <summary>
/// Distribution latente persistante d'une run — « l'inconscient du Palais ».
/// Évoluée par π(t+1)=π(t)×P (SPEC §8.2). Déterministe : dérivée de l'historique
/// de salles. Immuable.
/// </summary>
public sealed class RunPsyche
{
    private RunPsyche(MarkovStateDistribution distribution) => Distribution = distribution;

    public MarkovStateDistribution Distribution { get; }

    public static IReadOnlyList<EmotionalState> States { get; } =
        Enum.GetValues<EmotionalState>().ToArray();

    public static IReadOnlyList<MarkovState> MarkovStates { get; } =
        States.Select(s => MarkovState.Create(s.ToString())).ToArray();

    /// <summary>Distribution de Dirac initiale (SPEC §8.3) concentrée sur un état de base.</summary>
    public static RunPsyche Initial(EmotionalState baseline = EmotionalState.Calm) =>
        new(MarkovStateDistribution.FromSingleState(
            MarkovStates,
            MarkovState.Create(baseline.ToString())));

    public static RunPsyche From(MarkovStateDistribution distribution) => new(distribution);

    public decimal ProbabilityOf(EmotionalState state) =>
        Distribution[MarkovState.Create(state.ToString())];

    /// <summary>État dominant (départage déterministe par nom en cas d'égalité).</summary>
    public EmotionalState Dominant()
    {
        var top = Distribution.Probabilities
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key.Value, StringComparer.Ordinal)
            .First().Key;

        return Enum.Parse<EmotionalState>(top.Value, ignoreCase: true);
    }
}