using System.Globalization;

namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Represents a probability distribution over Markov states.
/// </summary>
public sealed class MarkovStateDistribution
{
    public const decimal ProbabilitySumTolerance = 0.000001m;

    private readonly IReadOnlyDictionary<MarkovState, decimal> _probabilities;

    private MarkovStateDistribution(IReadOnlyDictionary<MarkovState, decimal> probabilities)
    {
        _probabilities = probabilities;
    }

    public IReadOnlyDictionary<MarkovState, decimal> Probabilities => _probabilities;

    public decimal this[MarkovState state] => _probabilities[state];

    public static MarkovStateDistribution Create(IReadOnlyDictionary<MarkovState, decimal> probabilities)
    {
        ArgumentNullException.ThrowIfNull(probabilities);

        if (probabilities.Count == 0)
        {
            throw new MarkovMatrixValidationException("Markov state distribution cannot be empty.");
        }

        foreach (var probability in probabilities)
        {
            if (probability.Value < 0m || probability.Value > 1m)
            {
                throw new MarkovMatrixValidationException(
                    $"Distribution probability for state '{probability.Key}' must be between 0 and 1.");
            }
        }

        var sum = probabilities.Values.Sum();

        if (Math.Abs(sum - 1m) > ProbabilitySumTolerance)
        {
            throw new MarkovMatrixValidationException(
                $"Markov state distribution probabilities must sum to 1. Actual sum is {sum.ToString(CultureInfo.InvariantCulture)}.");
        }

        return new MarkovStateDistribution(
            new Dictionary<MarkovState, decimal>(probabilities));
    }

    public static MarkovStateDistribution FromSingleState(
        IReadOnlyCollection<MarkovState> states,
        MarkovState activeState)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(activeState);

        if (states.Count == 0)
        {
            throw new MarkovMatrixValidationException("Cannot create a distribution from an empty state set.");
        }

        if (!states.Contains(activeState))
        {
            throw new MarkovMatrixValidationException(
                $"Active state '{activeState}' is not part of the Markov state set.");
        }

        var probabilities = states.ToDictionary(
            state => state,
            state => state.Equals(activeState) ? 1m : 0m);

        return Create(probabilities);
    }
}