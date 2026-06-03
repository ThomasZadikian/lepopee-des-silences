namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Represents one row of a Markov transition matrix.
/// The row maps a source state to probabilities of moving to target states.
/// </summary>
public sealed class MarkovTransitionRow
{
    private readonly IReadOnlyDictionary<MarkovState, decimal> _probabilities;

    private MarkovTransitionRow(
        MarkovState sourceState,
        IReadOnlyDictionary<MarkovState, decimal> probabilities)
    {
        SourceState = sourceState;
        _probabilities = probabilities;
    }

    public MarkovState SourceState { get; }

    public IReadOnlyDictionary<MarkovState, decimal> Probabilities => _probabilities;

    public decimal GetProbabilityTo(MarkovState targetState)
    {
        ArgumentNullException.ThrowIfNull(targetState);

        return _probabilities[targetState];
    }

    public static MarkovTransitionRow Create(
        MarkovState sourceState,
        IReadOnlyDictionary<MarkovState, decimal> probabilities)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(probabilities);

        if (probabilities.Count == 0)
        {
            throw new MarkovMatrixValidationException(
                $"Transition row for state '{sourceState}' must contain at least one target probability.");
        }

        return new MarkovTransitionRow(
            sourceState,
            new Dictionary<MarkovState, decimal>(probabilities));
    }
}