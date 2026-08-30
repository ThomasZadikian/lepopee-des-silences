using System.Globalization;

namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Represents a finite Markov transition matrix P.
/// Each row must contain one transition probability for every known state.
/// Each row must sum to 1.
/// </summary>
public sealed class MarkovTransitionMatrix
{
    public const decimal RowSumTolerance = 0.000001m;

    private readonly IReadOnlyCollection<MarkovState> _states;
    private readonly IReadOnlyDictionary<MarkovState, MarkovTransitionRow> _rows;

    private MarkovTransitionMatrix(
        string key,
        string version,
        IReadOnlyCollection<MarkovState> states,
        IReadOnlyDictionary<MarkovState, MarkovTransitionRow> rows)
    {
        Key = key;
        Version = version;
        _states = states;
        _rows = rows;
    }

    public string Key { get; }

    public string Version { get; }

    public IReadOnlyCollection<MarkovState> States => _states;

    public IReadOnlyDictionary<MarkovState, MarkovTransitionRow> Rows => _rows;

    public MarkovTransitionRow this[MarkovState state] => _rows[state];

    public static MarkovTransitionMatrix Create(
        string key,
        string version,
        IReadOnlyCollection<MarkovState> states,
        IReadOnlyCollection<MarkovTransitionRow> rows)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new MarkovMatrixValidationException("Markov matrix key is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new MarkovMatrixValidationException("Markov matrix version is required.");
        }

        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(rows);

        if (states.Count == 0)
        {
            throw new MarkovMatrixValidationException("Markov matrix must contain at least one state.");
        }

        if (rows.Count == 0)
        {
            throw new MarkovMatrixValidationException("Markov matrix must contain at least one transition row.");
        }

        EnsureNoDuplicateStates(states);
        EnsureNoDuplicateRows(rows);

        var stateSet = states.ToHashSet();

        foreach (var state in states)
        {
            if (rows.All(row => !row.SourceState.Equals(state)))
            {
                throw new MarkovMatrixValidationException(
                    $"Missing transition row for state '{state}'.");
            }
        }

        foreach (var row in rows)
        {
            ValidateRow(row, stateSet);
        }

        return new MarkovTransitionMatrix(
            key.Trim(),
            version.Trim(),
            states.ToArray(),
            rows.ToDictionary(row => row.SourceState, row => row));
    }

    /// <summary>
    /// Applies the matrix multiplication π(t+1) = π(t) × P.
    /// </summary>
    public MarkovStateDistribution Advance(MarkovStateDistribution distribution)
    {
        ArgumentNullException.ThrowIfNull(distribution);

        EnsureDistributionMatchesMatrix(distribution);

        var nextProbabilities = _states.ToDictionary(
            state => state,
            _ => 0m);

        foreach (var sourceState in _states)
        {
            var sourceProbability = distribution[sourceState];
            var row = _rows[sourceState];

            foreach (var targetState in _states)
            {
                nextProbabilities[targetState] +=
                    sourceProbability * row.GetProbabilityTo(targetState);
            }
        }

        return MarkovStateDistribution.Create(nextProbabilities);
    }

    private static void EnsureNoDuplicateStates(IReadOnlyCollection<MarkovState> states)
    {
        var duplicatedState = states
            .GroupBy(state => state.Value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedState is not null)
        {
            throw new MarkovMatrixValidationException(
                $"Duplicate Markov state detected: '{duplicatedState.Key}'.");
        }
    }

    private static void EnsureNoDuplicateRows(IReadOnlyCollection<MarkovTransitionRow> rows)
    {
        var duplicatedRow = rows
            .GroupBy(row => row.SourceState)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedRow is not null)
        {
            throw new MarkovMatrixValidationException(
                $"Duplicate transition row detected for state '{duplicatedRow.Key}'.");
        }
    }

    private static void ValidateRow(
        MarkovTransitionRow row,
        IReadOnlySet<MarkovState> stateSet)
    {
        if (!stateSet.Contains(row.SourceState))
        {
            throw new MarkovMatrixValidationException(
                $"Transition row source state '{row.SourceState}' is not part of the matrix states.");
        }

        foreach (var state in stateSet)
        {
            if (!row.Probabilities.ContainsKey(state))
            {
                throw new MarkovMatrixValidationException(
                    $"Transition row for state '{row.SourceState}' is missing target state '{state}'.");
            }
        }

        foreach (var target in row.Probabilities)
        {
            if (!stateSet.Contains(target.Key))
            {
                throw new MarkovMatrixValidationException(
                    $"Transition row for state '{row.SourceState}' targets unknown state '{target.Key}'.");
            }

            if (target.Value < 0m || target.Value > 1m)
            {
                throw new MarkovMatrixValidationException(
                    $"Transition probability from '{row.SourceState}' to '{target.Key}' must be between 0 and 1.");
            }
        }

        var sum = row.Probabilities.Values.Sum();

        if (Math.Abs(sum - 1m) > RowSumTolerance)
        {
            throw new MarkovMatrixValidationException(
                $"Transition probabilities for state '{row.SourceState}' must sum to 1. Actual sum is {sum.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private void EnsureDistributionMatchesMatrix(MarkovStateDistribution distribution)
    {
        foreach (var state in _states)
        {
            if (!distribution.Probabilities.ContainsKey(state))
            {
                throw new MarkovMatrixValidationException(
                    $"Distribution is missing state '{state}'.");
            }
        }

        foreach (var state in distribution.Probabilities.Keys)
        {
            if (!_states.Contains(state))
            {
                throw new MarkovMatrixValidationException(
                    $"Distribution contains unknown state '{state}'.");
            }
        }
    }
}