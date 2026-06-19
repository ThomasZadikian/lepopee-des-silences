using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Leds.GameEngine.Domain.Selection;

public sealed class DeterministicWeightedSelector
{
    public SelectionResult Select(
        SelectionContext context,
        IReadOnlyCollection<SelectionCandidate> candidates,
        int maxSelections = 1,
        MarkovSelectionInfluence? markovInfluence = null)
    {
        if (candidates.Count == 0)
            return new SelectionResult([], []);

        var eligible = candidates.Where(c => c.Weight > 0).ToList();
        if (eligible.Count == 0)
            return new SelectionResult([], []);

        var weighted = eligible
            .Select(c => (Candidate: c, AdjustedWeight: ApplyMarkovInfluence(c, markovInfluence)))
            .Where(w => w.AdjustedWeight > 0)
            .ToList();

        var selected = new List<SelectionCandidate>();
        var available = weighted.ToList();

        for (var step = 0; step < maxSelections && available.Count > 0; step++)
        {
            var totalWeight = available.Sum(w => (long)w.AdjustedWeight);

            // Échantillon déterministe dans [0, totalWeight), stable entre processus,
            // machines et versions de .NET (SPEC §3 interdit new Random / §6 reproductibilité).
            var sample = NextUnitInterval(context.Seed, context.RunId, context.NodeId, step);
            var roll = (long)(sample * totalWeight);
            if (roll >= totalWeight)
                roll = totalWeight - 1; // garde-fou contre un arrondi en limite haute

            long cumulative = 0;
            var pickedIndex = 0;
            for (var j = 0; j < available.Count; j++)
            {
                cumulative += available[j].AdjustedWeight;
                if (roll < cumulative)
                {
                    pickedIndex = j;
                    break;
                }
            }

            selected.Add(available[pickedIndex].Candidate);
            available.RemoveAt(pickedIndex);
        }

        var decisions = selected.Select(c =>
            SelectionDecision.Create(
                context.RunId,
                context.DecisionType,
                context.ContextKey,
                c.Key,
                c.SelectionGroup,
                context.Seed,
                context.AlgorithmVersion,
                markovInfluence?.MatrixVersion,
                markovInfluence?.InfluenceTag)).ToList();

        return new SelectionResult(selected, decisions);
    }

    private static int ApplyMarkovInfluence(SelectionCandidate candidate, MarkovSelectionInfluence? influence)
    {
        if (influence is null)
            return candidate.Weight;

        var modifier = influence.GetModifier(candidate.Key);
        var adjusted = (int)Math.Round(candidate.Weight * modifier, MidpointRounding.AwayFromZero);
        return Math.Max(0, adjusted);
    }

    /// <summary>
    /// Réel pseudo-aléatoire déterministe dans [0, 1) (SHA-256, même modèle que
    /// DeterministicMarkovSampler). Aucun System.Random, aucun string.GetHashCode.
    /// </summary>
    private static decimal NextUnitInterval(string seed, Guid runId, Guid nodeId, int step)
    {
        var input = string.Join(
            '|',
            (seed ?? string.Empty).Trim(),
            runId.ToString("N"),
            nodeId.ToString("N"),
            step.ToString());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));

        return (decimal)(value / ((double)ulong.MaxValue + 1d));
    }
}

public sealed record SelectionResult(
    IReadOnlyCollection<SelectionCandidate> Selected,
    IReadOnlyCollection<SelectionDecision> Decisions);