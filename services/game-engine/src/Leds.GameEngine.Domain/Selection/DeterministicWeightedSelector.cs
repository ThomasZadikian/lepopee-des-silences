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

        var seedHash = ComputeSeedHash(context.Seed, context.RunId, context.NodeId);
        var rng = new Random(seedHash);

        var eligible = candidates.Where(c => c.Weight > 0).ToList();
        if (eligible.Count == 0)
            return new SelectionResult([], []);

        var weighted = eligible
            .Select(c => (Candidate: c, AdjustedWeight: ApplyMarkovInfluence(c, markovInfluence)))
            .Where(w => w.AdjustedWeight > 0)
            .ToList();

        var selected = new List<SelectionCandidate>();
        var available = weighted.ToList();

        for (var i = 0; i < maxSelections && available.Count > 0; i++)
        {
            var totalWeight = available.Sum(w => w.AdjustedWeight);
            var roll = rng.Next((int)totalWeight);
            var cumulative = 0;
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
        var adjusted = (int)Math.Round(candidate.Weight * modifier);
        return Math.Max(0, adjusted);
    }

    private static int ComputeSeedHash(string seed, Guid runId, Guid nodeId)
    {
        unchecked
        {
            var hash = seed.GetHashCode();
            hash = hash * 397 ^ runId.GetHashCode();
            hash = hash * 397 ^ nodeId.GetHashCode();
            return hash;
        }
    }
}

public sealed record SelectionResult(
    IReadOnlyCollection<SelectionCandidate> Selected,
    IReadOnlyCollection<SelectionDecision> Decisions);
