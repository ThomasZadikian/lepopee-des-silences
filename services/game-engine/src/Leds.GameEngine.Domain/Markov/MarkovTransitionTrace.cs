namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Trace auditable d'UNE décision de transition Markov (SPEC_MARKOV_ENGINE_V1 §10-§11).
/// Capture tout ce qui est nécessaire pour rejouer et expliquer un choix de génération :
/// la matrice + version, le scope (= les influenceurs : type, état, météo, lois...),
/// le seed, le step, l'état source, l'état cible et l'échantillon tiré.
/// Le moteur Markov ne persiste jamais cette trace lui-même : les modules de run
/// la collectent via IMarkovTransitionTraceSink.
/// </summary>
public sealed record MarkovTransitionTrace(
    string MatrixKey,
    string MatrixVersion,
    string Scope,
    string Seed,
    int Step,
    string SourceState,
    string TargetState,
    decimal Sample);