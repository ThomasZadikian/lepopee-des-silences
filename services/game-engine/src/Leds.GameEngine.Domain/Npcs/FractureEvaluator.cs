namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// Pure derivation of a wound's state from the relationship score and its authored
/// thresholds. Lower score = degraded relationship = more ruptured. Deterministic.
/// </summary>
public static class FractureEvaluator
{
    public static WoundState Evaluate(int relationshipScore, int tenseThreshold, int ruptureThreshold)
    {
        if (relationshipScore <= ruptureThreshold)
        {
            return WoundState.Rompu;
        }

        if (relationshipScore <= tenseThreshold)
        {
            return WoundState.Tendu;
        }

        return WoundState.Latent;
    }
}