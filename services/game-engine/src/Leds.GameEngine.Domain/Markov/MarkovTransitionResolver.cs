namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Resolves Markov transitions from a discrete current state.
/// </summary>
public sealed class MarkovTransitionResolver
{
    private readonly DeterministicMarkovSampler _sampler;

    public MarkovTransitionResolver(DeterministicMarkovSampler sampler)
    {
        _sampler = sampler;
    }

    public MarkovState ResolveNextState(
        MarkovTransitionMatrix matrix,
        MarkovState currentState,
        string seed,
        string scope,
        int step)
    {
        return ResolveWithTrace(matrix, currentState, seed, scope, step).NextState;
    }

    /// <summary>
    /// Comme <see cref="ResolveNextState(MarkovTransitionMatrix, MarkovState, string, string, int)"/>,
    /// mais renvoie aussi la trace auditable (sample, source, cible, version) — SPEC §11.
    /// </summary>
    public MarkovTransitionResolution ResolveWithTrace(
        MarkovTransitionMatrix matrix,
        MarkovState currentState,
        string seed,
        string scope,
        int step)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(currentState);

        if (!matrix.Rows.ContainsKey(currentState))
        {
            throw new MarkovMatrixValidationException(
                $"Current state '{currentState}' is not part of matrix '{matrix.Key}'.");
        }

        var sample = _sampler.NextUnitInterval(
            seed,
            scope,
            matrix.Key,
            matrix.Version,
            currentState.Value,
            step);

        var nextState = ResolveNextState(matrix, currentState, sample);

        var trace = new MarkovTransitionTrace(
            matrix.Key,
            matrix.Version,
            scope.Trim(),
            seed.Trim(),
            step,
            currentState.Value,
            nextState.Value,
            sample);

        return new MarkovTransitionResolution(nextState, trace);
    }

    public MarkovState ResolveNextState(
        MarkovTransitionMatrix matrix,
        MarkovState currentState,
        decimal sample)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(currentState);

        if (sample < 0m || sample >= 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sample),
                "Sample must be greater than or equal to 0 and strictly lower than 1.");
        }

        if (!matrix.Rows.TryGetValue(currentState, out var row))
        {
            throw new MarkovMatrixValidationException(
                $"Current state '{currentState}' is not part of matrix '{matrix.Key}'.");
        }

        var cumulative = 0m;

        foreach (var targetState in matrix.States)
        {
            cumulative += row.GetProbabilityTo(targetState);

            if (sample < cumulative)
            {
                return targetState;
            }
        }

        return matrix.States.Last();
    }
}