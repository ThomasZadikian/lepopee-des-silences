using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Infrastructure.Services;

public static class CompanionMarkovService
{
    private static readonly Random _rng = new();

    public static string ComputeNextState(CompanionState current, int victoriesToday, int defeatesToday)
    {
        var timeSinceLastCombat = DateTime.UtcNow - current.LastCombatAt;

        // Matrice de transition de base
        var transitions = new Dictionary<string, Dictionary<string, float>>
        {
            [Constants.CompanionStateRepos] = new()
            {
                [Constants.CompanionStateRepos] = 0.30f,
                [Constants.CompanionStateJeu] = 0.25f,
                [Constants.CompanionStateManger] = 0.20f,
                [Constants.CompanionStateExcite] = 0.10f,
                [Constants.CompanionStateTriste] = 0.05f,
                [Constants.CompanionStateEndormi] = 0.10f,
            },
            [Constants.CompanionStateJeu] = new()
            {
                [Constants.CompanionStateRepos] = 0.30f,
                [Constants.CompanionStateJeu] = 0.25f,
                [Constants.CompanionStateManger] = 0.20f,
                [Constants.CompanionStateExcite] = 0.15f,
                [Constants.CompanionStateTriste] = 0.05f,
                [Constants.CompanionStateEndormi] = 0.05f,
            },
            [Constants.CompanionStateManger] = new()
            {
                [Constants.CompanionStateRepos] = 0.40f,
                [Constants.CompanionStateJeu] = 0.20f,
                [Constants.CompanionStateManger] = 0.10f,
                [Constants.CompanionStateExcite] = 0.10f,
                [Constants.CompanionStateTriste] = 0.10f,
                [Constants.CompanionStateEndormi] = 0.10f,
            },
            [Constants.CompanionStateExcite] = new()
            {
                [Constants.CompanionStateRepos] = 0.20f,
                [Constants.CompanionStateJeu] = 0.35f,
                [Constants.CompanionStateManger] = 0.15f,
                [Constants.CompanionStateExcite] = 0.20f,
                [Constants.CompanionStateTriste] = 0.05f,
                [Constants.CompanionStateEndormi] = 0.05f,
            },
            [Constants.CompanionStateTriste] = new()
            {
                [Constants.CompanionStateRepos] = 0.30f,
                [Constants.CompanionStateJeu] = 0.10f,
                [Constants.CompanionStateManger] = 0.15f,
                [Constants.CompanionStateExcite] = 0.05f,
                [Constants.CompanionStateTriste] = 0.30f,
                [Constants.CompanionStateEndormi] = 0.10f,
            },
            [Constants.CompanionStateEndormi] = new()
            {
                [Constants.CompanionStateRepos] = 0.50f,
                [Constants.CompanionStateJeu] = 0.15f,
                [Constants.CompanionStateManger] = 0.15f,
                [Constants.CompanionStateExcite] = 0.05f,
                [Constants.CompanionStateTriste] = 0.05f,
                [Constants.CompanionStateEndormi] = 0.10f,
            },
        };

        var probs = new Dictionary<string, float>(
            transitions.TryGetValue(current.CurrentState, out var t)
                ? t
                : transitions[Constants.CompanionStateRepos]);

        // Modificateurs selon les stats de combat
        if (victoriesToday > 10)
            ApplyBoost(probs, Constants.CompanionStateExcite, 0.20f);

        if (defeatesToday > 5)
            ApplyBoost(probs, Constants.CompanionStateTriste, 0.20f);

        if (timeSinceLastCombat.TotalHours > 2)
            ApplyBoost(probs, Constants.CompanionStateEndormi, 0.15f);

        // Normalise les probabilités
        Normalize(probs);

        return PickState(probs);
    }

    private static void ApplyBoost(Dictionary<string, float> probs, string state, float boost)
    {
        if (!probs.ContainsKey(state)) return;
        probs[state] += boost;
    }

    private static void Normalize(Dictionary<string, float> probs)
    {
        var total = probs.Values.Sum();
        foreach (var key in probs.Keys.ToList())
            probs[key] /= total;
    }

    private static string PickState(Dictionary<string, float> probs)
    {
        var roll = (float)_rng.NextDouble();
        var cumulative = 0f;
        foreach (var (state, prob) in probs)
        {
            cumulative += prob;
            if (roll <= cumulative) return state;
        }
        return probs.Keys.Last();
    }
}