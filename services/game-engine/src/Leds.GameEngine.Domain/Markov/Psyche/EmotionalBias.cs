namespace Leds.GameEngine.Domain.Markov.Psyche;

/// <summary>
/// Biais de poids déterministe : mélange une matrice de transition de base avec une
/// préférence (projetée depuis la psyché) sur le MÊME espace d'états cibles.
/// Ligne effective = (1−α)·base + α·préférence, renormalisée → reste une vraie matrice
/// Markov valide (chaque ligne somme à 1). C'est le levier "poids" du biais combiné.
/// </summary>
public static class EmotionalBias
{
    public static MarkovTransitionMatrix ApplyPreference(
        MarkovTransitionMatrix baseMatrix,
        IReadOnlyDictionary<MarkovState, decimal> preference,
        decimal alpha)
    {
        ArgumentNullException.ThrowIfNull(baseMatrix);
        ArgumentNullException.ThrowIfNull(preference);

        if (alpha <= 0m)
            return baseMatrix;

        var states = baseMatrix.States;
        var prefSum = states.Sum(s => preference.TryGetValue(s, out var p) ? p : 0m);

        var rows = new List<MarkovTransitionRow>();

        foreach (var source in states)
        {
            var baseRow = baseMatrix[source];
            var mixed = new Dictionary<MarkovState, decimal>();
            var sum = 0m;

            foreach (var target in states)
            {
                var b = baseRow.GetProbabilityTo(target);
                var pref = prefSum > 0m
                    ? (preference.TryGetValue(target, out var pv) ? pv : 0m) / prefSum
                    : 0m;

                var value = (1m - alpha) * b + alpha * pref;
                mixed[target] = value;
                sum += value;
            }

            // Renormalisation défensive → garantit somme==1 dans la tolérance Markov.
            foreach (var target in states)
                mixed[target] = sum > 0m ? mixed[target] / sum : baseRow.GetProbabilityTo(target);

            rows.Add(MarkovTransitionRow.Create(source, mixed));
        }

        // Version suffixée → la trace (SPEC §11) reflète honnêtement que la matrice est biaisée.
        return MarkovTransitionMatrix.Create(
            baseMatrix.Key,
            baseMatrix.Version + "+psyche",
            states,
            rows);
    }
}