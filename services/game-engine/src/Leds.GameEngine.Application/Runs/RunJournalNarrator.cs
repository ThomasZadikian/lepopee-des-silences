namespace Leds.GameEngine.Application.Runs;

/// <summary>
/// Builds the literary journal lines written into a run's Carnet de bord
/// (<see cref="Domain.Runs.Run.AppendJournalEntry"/>). Kept separate from the handlers that
/// call it so the phrasing has one place to live and to test.
/// </summary>
public static class RunJournalNarrator
{
    public static string DescribeItemFound(string? roomDisplayName, string itemDisplayName)
    {
        var room = string.IsNullOrWhiteSpace(roomDisplayName) ? "cette pièce" : roomDisplayName;
        return $"J'ai trouvé un objet abandonné dans {room}, c'était {itemDisplayName}.";
    }

    public static string DescribeCombatVictory(IReadOnlyCollection<string> enemyDisplayNames)
    {
        var foes = DescribeEnemies(enemyDisplayNames);
        return enemyDisplayNames.Count > 1
            ? $"J'ai combattu {foes}. Je les ai vaincus."
            : $"J'ai combattu {foes}. Je l'ai vaincu.";
    }

    public static string DescribeCombatDefeat(IReadOnlyCollection<string> enemyDisplayNames)
    {
        var foes = DescribeEnemies(enemyDisplayNames);
        return $"J'ai affronté {foes}. Je n'ai pas survécu à ce combat.";
    }

    private static string DescribeEnemies(IReadOnlyCollection<string> names)
    {
        if (names.Count == 0)
        {
            return "un ennemi";
        }

        if (names.Count == 1)
        {
            return names.First();
        }

        return string.Join(", ", names.Take(names.Count - 1)) + " et " + names.Last();
    }
}
