namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Résultat d'une transition discrète : l'état suivant résolu + sa trace auditable.
/// </summary>
public sealed record MarkovTransitionResolution(
    MarkovState NextState,
    MarkovTransitionTrace Trace);