using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Psyche;

/// <summary>
/// SURFACE DE CALIBRATION PROPRIÉTAIRE. Toutes les probabilités, mappings et
/// intensités du mécanisme de psyché vivent ICI. Ce sont des décisions de game
/// design (le "feel" du Palais) : ajuste-les librement.
/// Invariant : chaque distribution doit sommer à 1.
/// </summary>
public sealed class EmotionalCalibration
{
    // ── Intensités du biais (À CALIBRER) ───────────────────────────────────────
    public const decimal RoomTypeBiasAlpha = 0.35m;   // poids de la psyché sur le TYPE de salle
    public const decimal PalaceStateBiasAlpha = 0.35m; // poids de la psyché sur l'ÉTAT de salle
    public const decimal NudgeStrengthBeta = 0.25m;    // force du "nudge" d'une salle traversée

    public const string TendencyMatrixKey = "emotional-tendency";
    public const string TendencyMatrixVersion = "markov-emotional-tendency-0.1.0";

    // ── 1) Matrice de tendance P (auto-influence, SPEC §8.2) ────────────────────
    // Diagonale forte = persistance ; fuite = dérive. À CALIBRER.
    public MarkovTransitionMatrix BuildTendencyMatrix()
    {
        MarkovState S(EmotionalState e) => MarkovState.Create(e.ToString());

        var rows = new[]
        {
            Row(EmotionalState.Calm,        (Calm:.55m, Wary:.15m, Withdrawn:.10m, Dissociated:.05m, Fragmented:.05m, Lucid:.10m)),
            Row(EmotionalState.Wary,        (Calm:.15m, Wary:.45m, Withdrawn:.15m, Dissociated:.10m, Fragmented:.10m, Lucid:.05m)),
            Row(EmotionalState.Withdrawn,   (Calm:.10m, Wary:.15m, Withdrawn:.45m, Dissociated:.15m, Fragmented:.10m, Lucid:.05m)),
            Row(EmotionalState.Dissociated, (Calm:.05m, Wary:.10m, Withdrawn:.20m, Dissociated:.45m, Fragmented:.15m, Lucid:.05m)),
            Row(EmotionalState.Fragmented,  (Calm:.05m, Wary:.10m, Withdrawn:.15m, Dissociated:.20m, Fragmented:.45m, Lucid:.05m)),
            Row(EmotionalState.Lucid,       (Calm:.20m, Wary:.10m, Withdrawn:.10m, Dissociated:.05m, Fragmented:.05m, Lucid:.50m)),
        };

        return MarkovTransitionMatrix.Create(
            TendencyMatrixKey, TendencyMatrixVersion, RunPsyche.MarkovStates, rows);

        MarkovTransitionRow Row(
            EmotionalState src,
            (decimal Calm, decimal Wary, decimal Withdrawn, decimal Dissociated, decimal Fragmented, decimal Lucid) p) =>
            MarkovTransitionRow.Create(S(src), new Dictionary<MarkovState, decimal>
            {
                [S(EmotionalState.Calm)] = p.Calm,
                [S(EmotionalState.Wary)] = p.Wary,
                [S(EmotionalState.Withdrawn)] = p.Withdrawn,
                [S(EmotionalState.Dissociated)] = p.Dissociated,
                [S(EmotionalState.Fragmented)] = p.Fragmented,
                [S(EmotionalState.Lucid)] = p.Lucid,
            });
    }

    // ── 2) Nudge : comment une salle TRAVERSÉE pousse la psyché (accumulation) ───
    // À CALIBRER. (Extension possible : tenir compte aussi de room.RoomType / des choix / des Lois.)
    public MarkovStateDistribution Nudge(MarkovStateDistribution current, Room room)
    {
        var target = NudgeTargetFor(room.PalaceState);
        var mixed = new Dictionary<MarkovState, decimal>();
        var sum = 0m;

        foreach (var s in RunPsyche.MarkovStates)
        {
            var c = current[s];
            var t = target.TryGetValue(s, out var tv) ? tv : 0m;
            var v = (1m - NudgeStrengthBeta) * c + NudgeStrengthBeta * t;
            mixed[s] = v;
            sum += v;
        }

        foreach (var s in RunPsyche.MarkovStates)
            mixed[s] = mixed[s] / sum;

        return MarkovStateDistribution.Create(mixed);
    }

    private static IReadOnlyDictionary<MarkovState, decimal> NudgeTargetFor(PalaceRoomState state)
    {
        MarkovState E(EmotionalState e) => MarkovState.Create(e.ToString());

        return state switch
        {
            PalaceRoomState.Silent => new Dictionary<MarkovState, decimal>
            { [E(EmotionalState.Withdrawn)] = .5m, [E(EmotionalState.Dissociated)] = .5m },
            PalaceRoomState.Painful => new Dictionary<MarkovState, decimal>
            { [E(EmotionalState.Fragmented)] = .6m, [E(EmotionalState.Dissociated)] = .4m },
            _ => new Dictionary<MarkovState, decimal>
            { [E(EmotionalState.Calm)] = .6m, [E(EmotionalState.Lucid)] = .4m },
        };
    }

    // ── 3) Projection psyché → préférence sur les TYPES de salle (biais α) ───────
    public IReadOnlyDictionary<MarkovState, decimal> ProjectToRoomTypes(RunPsyche psyche)
    {
        var acc = new[] { "Threshold", "Memory", "Forest", "Rupture", "Silence", "Antechamber" }
            .ToDictionary(MarkovState.Create, _ => 0m);

        foreach (var emo in RunPsyche.States)
        {
            var w = psyche.ProbabilityOf(emo);
            if (w <= 0m) continue;
            foreach (var (name, p) in RoomTypePrefFor(emo))
                acc[MarkovState.Create(name)] += w * p;
        }

        return acc;
    }

    // À CALIBRER : quelle émotion attire vers quel type de salle (somme = 1 par émotion).
    private static IEnumerable<(string Type, decimal Weight)> RoomTypePrefFor(EmotionalState e) => e switch
    {
        EmotionalState.Calm => [("Memory", .4m), ("Forest", .4m), ("Antechamber", .2m)],
        EmotionalState.Wary => [("Antechamber", .4m), ("Rupture", .3m), ("Silence", .3m)],
        EmotionalState.Withdrawn => [("Silence", .5m), ("Memory", .3m), ("Antechamber", .2m)],
        EmotionalState.Dissociated => [("Silence", .4m), ("Rupture", .4m), ("Memory", .2m)],
        EmotionalState.Fragmented => [("Rupture", .5m), ("Silence", .3m), ("Forest", .2m)],
        EmotionalState.Lucid => [("Memory", .5m), ("Forest", .3m), ("Antechamber", .2m)],
        _ => [("Memory", 1m)],
    };

    // ── 4) Projection psyché → préférence sur les ÉTATS de salle (biais α) ───────
    public IReadOnlyDictionary<MarkovState, decimal> ProjectToPalaceStates(RunPsyche psyche)
    {
        var acc = new[] { "Neutral", "Silent", "Painful" }
            .ToDictionary(MarkovState.Create, _ => 0m);

        foreach (var emo in RunPsyche.States)
        {
            var w = psyche.ProbabilityOf(emo);
            if (w <= 0m) continue;
            foreach (var (name, p) in PalaceStatePrefFor(emo))
                acc[MarkovState.Create(name)] += w * p;
        }

        return acc;
    }

    // À CALIBRER.
    private static IEnumerable<(string State, decimal Weight)> PalaceStatePrefFor(EmotionalState e) => e switch
    {
        EmotionalState.Calm => [("Neutral", .7m), ("Silent", .2m), ("Painful", .1m)],
        EmotionalState.Wary => [("Neutral", .4m), ("Silent", .4m), ("Painful", .2m)],
        EmotionalState.Withdrawn => [("Silent", .6m), ("Neutral", .3m), ("Painful", .1m)],
        EmotionalState.Dissociated => [("Silent", .5m), ("Painful", .4m), ("Neutral", .1m)],
        EmotionalState.Fragmented => [("Painful", .6m), ("Silent", .3m), ("Neutral", .1m)],
        EmotionalState.Lucid => [("Neutral", .6m), ("Silent", .3m), ("Painful", .1m)],
        _ => [("Neutral", 1m)],
    };
}