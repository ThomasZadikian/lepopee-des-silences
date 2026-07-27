namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Où une unité peut aller, et par quel chemin.
/// </summary>
/// <remarks>
/// Séparé de <see cref="TacticalBattlefield"/> parce que le terrain seul ne suffit pas : une case
/// praticable peut être occupée par un combattant vivant, et l'occupation change à chaque
/// résolution. Le terrain est figé, l'accessibilité ne l'est pas.
/// </remarks>
public static class TacticalMovement
{
    /// <summary>
    /// Budget de déplacement d'un combattant : une base d'archétype, plus un bonus dérivé de la
    /// Vitesse. La référence est 10 — en dessous on est ralenti, au-dessus on gagne une case tous
    /// les quatre points (cf. SFD v2, §9).
    /// </summary>
    /// <remarks>// BALANCE KNOB</remarks>
    public const int SpeedReference = 10;

    /// <inheritdoc cref="SpeedReference"/>
    public const int SpeedPointsPerExtraCell = 4;

    /// <summary>Budget de base, faute d'archétype connu. // BALANCE KNOB</summary>
    public const int BaseMovement = 4;

    public static int BudgetFor(int effectiveSpeed, int baseMovement = BaseMovement)
    {
        // Division entière vers moins l'infini : un combattant plus lent que la référence perd
        // bien des cases au lieu d'être arrondi vers le haut par troncature.
        var delta = effectiveSpeed - SpeedReference;
        var bonus = (int)Math.Floor(delta / (double)SpeedPointsPerExtraCell);
        return Math.Max(1, baseMovement + bonus);
    }

    /// <summary>
    /// Cases atteignables depuis <paramref name="origin"/> avec <paramref name="budget"/> points,
    /// et leur coût. L'origine y figure, à coût nul.
    /// </summary>
    /// <param name="occupied">
    /// Cases tenues par un combattant vivant. Elles bloquent le passage <b>et</b> l'arrivée : on
    /// ne traverse pas quelqu'un, et deux unités ne partagent jamais une case.
    /// </param>
    public static IReadOnlyDictionary<GridPosition, int> ReachableCells(
        TacticalBattlefield battlefield,
        GridPosition origin,
        int budget,
        IReadOnlySet<GridPosition> occupied)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(occupied);

        var best = new Dictionary<GridPosition, int> { [origin] = 0 };
        if (budget <= 0)
            return best;

        // Dijkstra plutôt qu'un simple parcours en largeur : le coût d'une case n'est pas
        // uniforme (l'élévation le fait varier), donc le premier chemin trouvé n'est pas
        // nécessairement le moins cher.
        var frontier = new PriorityQueue<GridPosition, int>();
        frontier.Enqueue(origin, 0);

        while (frontier.TryDequeue(out var current, out var currentCost))
        {
            // Entrée périmée : une route moins chère vers cette case a déjà été traitée.
            if (currentCost > best.GetValueOrDefault(current, int.MaxValue))
                continue;

            foreach (var neighbour in current.Neighbours())
            {
                if (!battlefield.IsWalkable(neighbour) || occupied.Contains(neighbour))
                    continue;

                var cost = currentCost + battlefield.StepCost(current, neighbour);
                if (cost > budget)
                    continue;

                if (cost >= best.GetValueOrDefault(neighbour, int.MaxValue))
                    continue;

                best[neighbour] = cost;
                frontier.Enqueue(neighbour, cost);
            }
        }

        return best;
    }

    /// <summary>
    /// Ligne de vue entre deux cases. Coupée par une case non praticable, ou par une crête
    /// strictement plus haute que ses deux extrémités — une butte entre deux tireurs les sépare,
    /// mais tirer depuis ou vers le sommet reste possible (cf. SFD v2, §10).
    /// </summary>
    public static bool HasLineOfSight(
        TacticalBattlefield battlefield, GridPosition from, GridPosition to)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        // Au contact, la vue est toujours acquise : rien ne tient entre deux cases adjacentes.
        if (from.ManhattanDistanceTo(to) <= 1)
            return true;

        var ceiling = Math.Max(battlefield.ElevationAt(from), battlefield.ElevationAt(to));

        foreach (var cell in TraceLine(from, to))
        {
            if (cell == from || cell == to)
                continue;

            if (!battlefield.IsWalkable(cell))
                return false;

            if (battlefield.ElevationAt(cell) > ceiling)
                return false;
        }

        return true;
    }

    /// <summary>Bresenham arrondi : les cases traversées par le segment, extrémités comprises.</summary>
    private static IEnumerable<GridPosition> TraceLine(GridPosition from, GridPosition to)
    {
        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        var steps = Math.Max(dx, dy);

        if (steps == 0)
        {
            yield return from;
            yield break;
        }

        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var x = (int)Math.Round(from.X + ((to.X - from.X) * t), MidpointRounding.AwayFromZero);
            var y = (int)Math.Round(from.Y + ((to.Y - from.Y) * t), MidpointRounding.AwayFromZero);
            yield return new GridPosition(x, y);
        }
    }
}
