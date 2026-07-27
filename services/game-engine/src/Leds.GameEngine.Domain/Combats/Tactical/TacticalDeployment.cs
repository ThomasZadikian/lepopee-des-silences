using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Où les deux camps se posent quand le combat s'engage (cf. SFD v2, §6).
/// </summary>
/// <remarks>
/// <para>
/// Entièrement <b>ordinal</b> : aucun tirage aléatoire. Les cases candidates sont triées par
/// distance puis par coordonnées, et les premières sont prises. Un même terrain et un même
/// groupe donnent donc toujours le même déploiement, sans avoir à porter une graine jusqu'ici
/// ni à s'inquiéter de la reproductibilité d'une reprise de sauvegarde.
/// </para>
/// <para>
/// Le placement ennemi suit le <b>rôle</b>, qui est déjà authoré sur chaque créature. C'est ce
/// qui donne son caractère à une famille sans exiger de nouveau contenu : les Veilleurs, faits
/// de Guard et de Support, se posent naturellement en barrage avec leurs porteurs en retrait,
/// là où les Chimères, faites de Skirmisher, arrivent dispersées. Un placement authoré famille
/// par famille pourra affiner plus tard, sans rien changer ici.
/// </para>
/// </remarks>
public static class TacticalDeployment
{
    /// <summary>Distance minimale entre les deux camps à l'engagement. // BALANCE KNOB</summary>
    public const int MinEngagementDistance = 8;

    /// <summary>Distance maximale. // BALANCE KNOB</summary>
    public const int MaxEngagementDistance = 20;

    /// <summary>
    /// Distance à laquelle chaque rôle préfère se poser, à l'intérieur de la fourchette
    /// d'engagement. 0 = au plus près des alliés, 1 = au plus loin.
    /// </summary>
    /// <remarks>// BALANCE KNOB</remarks>
    private static double PreferredBandFor(string role) => role.ToLowerInvariant() switch
    {
        "guard" => 0.0,       // barrage : ils viennent au contact
        "bruiser" => 0.15,
        "swarm" => 0.3,       // le nombre compense l'approche
        "skirmisher" => 0.45,
        "disruptor" => 0.7,
        "drain" => 0.75,
        "support" => 0.9,     // toujours derrière ce qu'ils soutiennent
        _ => 0.5,
    };

    /// <summary>
    /// Pose les alliés autour de <paramref name="anchor"/> — la case du jeton de groupe au moment
    /// du déclenchement. Le groupe, entité unique en exploration, se dépiaute ici.
    /// </summary>
    /// <exception cref="DomainException">
    /// Si le terrain n'offre pas assez de cases praticables autour de l'ancre. Ce n'est pas un cas
    /// à absorber en silence : un combat où une partie de l'équipe n'existe pas serait pire que
    /// l'échec de génération.
    /// </exception>
    public static IReadOnlyList<GridPosition> DeployAllies(
        TacticalBattlefield battlefield, GridPosition anchor, int count)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        if (count <= 0)
            throw new DomainException("At least one ally must be deployed.");

        var placed = new List<GridPosition>();
        var taken = new HashSet<GridPosition>();

        // L'ancre elle-même d'abord quand elle est praticable : le porteur reste là où le joueur
        // avait mené le groupe, ce qui rend la transition lisible.
        if (battlefield.IsWalkable(anchor))
        {
            placed.Add(anchor);
            taken.Add(anchor);
        }

        foreach (var cell in CellsByProximity(battlefield, anchor))
        {
            if (placed.Count >= count)
                break;

            if (taken.Contains(cell))
                continue;

            placed.Add(cell);
            taken.Add(cell);
        }

        if (placed.Count < count)
            throw new DomainException(
                $"The battlefield offers only {placed.Count} walkable cells around {anchor}; "
                + $"{count} allies must be deployed.");

        return placed;
    }

    /// <summary>
    /// Pose les ennemis face aux alliés, chacun selon son rôle. <paramref name="enemyRoles"/> est
    /// dans l'ordre des ennemis à placer et donne le rôle de chacun.
    /// </summary>
    /// <remarks>
    /// La fourchette d'engagement se resserre d'elle-même si le terrain ne l'offre pas : une salle
    /// trop petite donne un déploiement dégradé plutôt qu'un échec (SFD v2, §4). Un combat qui
    /// démarre trop serré reste jouable ; un combat qui refuse de démarrer bloque la run.
    /// </remarks>
    public static IReadOnlyList<GridPosition> DeployEnemies(
        TacticalBattlefield battlefield,
        IReadOnlyCollection<GridPosition> allyPositions,
        IReadOnlyList<string> enemyRoles)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(allyPositions);
        ArgumentNullException.ThrowIfNull(enemyRoles);

        if (allyPositions.Count == 0)
            throw new DomainException("Enemies cannot be deployed before the allies are placed.");
        if (enemyRoles.Count == 0)
            throw new DomainException("At least one enemy must be deployed.");

        var taken = new HashSet<GridPosition>(allyPositions);
        var candidates = EngagementCells(battlefield, allyPositions);

        if (candidates.Count < enemyRoles.Count)
            throw new DomainException(
                $"The battlefield offers only {candidates.Count} cells to deploy "
                + $"{enemyRoles.Count} enemies.");

        var nearest = candidates.Min(c => c.Distance);
        var farthest = candidates.Max(c => c.Distance);
        var span = Math.Max(1, farthest - nearest);

        var placed = new List<GridPosition>();

        foreach (var role in enemyRoles)
        {
            var target = nearest + (PreferredBandFor(role) * span);

            var pick = candidates
                .Where(c => !taken.Contains(c.Cell))
                // L'écart à la distance voulue prime ; les coordonnées ne servent qu'à départager
                // pour que le résultat soit reproductible.
                .OrderBy(c => Math.Abs(c.Distance - target))
                .ThenBy(c => c.Cell.Y)
                .ThenBy(c => c.Cell.X)
                .Select(c => (GridPosition?)c.Cell)
                .FirstOrDefault();

            if (pick is null)
                throw new DomainException("Ran out of free cells while deploying enemies.");

            placed.Add(pick.Value);
            taken.Add(pick.Value);
        }

        return placed;
    }

    /// <summary>
    /// Cases éligibles au déploiement ennemi, avec leur distance au plus proche allié. La
    /// fourchette idéale d'abord ; si elle ne donne rien d'exploitable, on la relâche par paliers
    /// plutôt que d'échouer.
    /// </summary>
    private static List<(GridPosition Cell, int Distance)> EngagementCells(
        TacticalBattlefield battlefield, IReadOnlyCollection<GridPosition> allyPositions)
    {
        var all = new List<(GridPosition Cell, int Distance)>();

        for (var y = 0; y < battlefield.Height; y++)
        {
            for (var x = 0; x < battlefield.Width; x++)
            {
                var cell = new GridPosition(x, y);
                if (!battlefield.IsWalkable(cell))
                    continue;

                all.Add((cell, allyPositions.Min(a => a.ManhattanDistanceTo(cell))));
            }
        }

        var ideal = all
            .Where(c => c.Distance is >= MinEngagementDistance and <= MaxEngagementDistance)
            .ToList();

        if (ideal.Count > 0)
            return ideal;

        // Repli : on garde l'exigence d'un minimum d'espace entre les camps et on la baisse
        // jusqu'à trouver de la place. Le plancher est 2 — au contact immédiat, le déplacement
        // n'aurait plus aucun rôle et le combat tactique perdrait son objet.
        for (var floor = MinEngagementDistance - 1; floor >= 2; floor--)
        {
            var relaxed = all.Where(c => c.Distance >= floor).ToList();
            if (relaxed.Count > 0)
                return relaxed;
        }

        return all.Where(c => c.Distance > 0).ToList();
    }

    /// <summary>
    /// Cases praticables triées par proximité à l'ancre. Les coordonnées départagent les
    /// ex æquo pour que l'ordre soit stable.
    /// </summary>
    private static IEnumerable<GridPosition> CellsByProximity(
        TacticalBattlefield battlefield, GridPosition anchor)
    {
        var cells = new List<GridPosition>();

        for (var y = 0; y < battlefield.Height; y++)
        {
            for (var x = 0; x < battlefield.Width; x++)
            {
                var cell = new GridPosition(x, y);
                if (battlefield.IsWalkable(cell))
                    cells.Add(cell);
            }
        }

        return cells
            .OrderBy(c => c.ManhattanDistanceTo(anchor))
            .ThenBy(c => c.Y)
            .ThenBy(c => c.X);
    }
}
