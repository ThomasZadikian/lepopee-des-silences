using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.Infrastructure.Markov;

/// <summary>
/// Implémentation par défaut : ne fait rien. Garantit zéro changement de comportement
/// tant que la persistance/projection des traces n'est pas branchée.
/// </summary>
public sealed class NullMarkovTransitionTraceSink : IMarkovTransitionTraceSink
{
    public void Record(MarkovTransitionTrace trace)
    {
        // no-op
    }
}