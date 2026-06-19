namespace Leds.GameEngine.Domain.Markov.Psyche;

/// <summary>
/// États narratifs de la psyché d'une run (SPEC §5, vocabulaire non-médical).
/// Élargis-les librement : ajoute une valeur ici + sa ligne dans la matrice de
/// tendance et ses préférences dans EmotionalCalibration.
/// </summary>
public enum EmotionalState
{
    Calm,
    Wary,
    Withdrawn,
    Dissociated,
    Fragmented,
    Lucid
}