using System.Collections.Concurrent;
using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.Infrastructure.Markov;

/// <summary>
/// Collecte les traces Markov en mémoire (thread-safe). Utile en développement et
/// pour les tests de reproductibilité. Non destiné à la production telle quelle.
/// </summary>
public sealed class InMemoryMarkovTransitionTraceSink : IMarkovTransitionTraceSink
{
    private readonly ConcurrentQueue<MarkovTransitionTrace> _traces = new();

    public void Record(MarkovTransitionTrace trace)
    {
        if (trace is not null)
            _traces.Enqueue(trace);
    }

    public IReadOnlyCollection<MarkovTransitionTrace> Snapshot() => _traces.ToArray();

    public void Clear() => _traces.Clear();
}