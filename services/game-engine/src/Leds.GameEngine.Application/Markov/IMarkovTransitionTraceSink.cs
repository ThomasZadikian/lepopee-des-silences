using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.Application.Markov;

/// <summary>
/// Reçoit les traces de transition Markov émises pendant la génération d'une run,
/// afin de les auditer, rejouer ou projeter (SPEC §11). Appelé depuis des chemins de
/// génération : une implémentation ne doit jamais lever d'exception.
/// </summary>
public interface IMarkovTransitionTraceSink
{
    void Record(MarkovTransitionTrace trace);
}