using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Les formes de zone autorisées en combat tactique (cf. SFD v2, §10).
/// </summary>
/// <remarks>
/// Quatre, et pas une de plus. La forme doit être reconnue avant la couleur : la surbrillance
/// annonce la zone, l'effet la confirme, il ne la contredit jamais. Ajouter une cinquième forme
/// obligerait à réapprendre une lecture que le joueur tient déjà.
/// </remarks>
public enum TacticalAreaShape
{
    /// <summary>La seule case ciblée.</summary>
    Single = 0,

    /// <summary>La case et ses quatre voisines — distance de Manhattan ≤ 1.</summary>
    Cross = 1,

    /// <summary>Losange de rayon 2 — distance de Manhattan ≤ 2.</summary>
    Diamond = 2,

    /// <summary>
    /// Toute la carte, sans distinction de camp. Symétrique par construction : c'est le levier
    /// de risque de la SFD rendu littéral, et il n'épargne personne.
    /// </summary>
    Map = 3,
}

/// <summary>
/// Qui une compétence peut atteindre, et depuis où.
/// </summary>
/// <remarks>
/// <para>
/// Les modes de ciblage du catalogue — <c>SingleEnemy</c>, <c>AllEnemies</c>, <c>AllAllies</c>,
/// <c>SingleAlly</c>, <c>Self</c> — ont été écrits pour l'ATB, où « tous les ennemis » veut dire
/// tous ceux du combat. Sur une grille, cette lecture viderait le positionnement de son sens :
/// un sort de zone toucherait autant en se tenant à l'écart qu'au cœur de la mêlée.
/// </para>
/// <para>
/// Ils sont donc <b>réinterprétés spatialement</b> : « tous les ennemis » devient « tous les
/// ennemis dans la zone d'effet ». Seule une compétence explicitement déclarée
/// <see cref="TacticalAreaShape.Map"/> retrouve la portée du combat entier — et elle la paie en
/// touchant aussi son propre camp.
/// </para>
/// </remarks>
public static class TacticalTargeting
{
    /// <summary>
    /// Les cases couvertes par une zone centrée sur <paramref name="center"/>. Les cases hors du
    /// champ de bataille sont écartées ; les cases non praticables sont conservées, parce qu'un
    /// souffle passe au-dessus d'un éboulis même si personne ne peut s'y tenir.
    /// </summary>
    /// <exception cref="DomainException">
    /// Pour <see cref="TacticalAreaShape.Map"/>, qui ne se calcule pas depuis un centre :
    /// l'appelant connaît déjà l'ensemble des combattants.
    /// </exception>
    public static IReadOnlyCollection<GridPosition> CellsInArea(
        TacticalBattlefield battlefield, GridPosition center, TacticalAreaShape shape)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        var radius = shape switch
        {
            TacticalAreaShape.Single => 0,
            TacticalAreaShape.Cross => 1,
            TacticalAreaShape.Diamond => 2,
            TacticalAreaShape.Map => throw new DomainException(
                "A map-wide skill has no area centre: it reaches every combatant on the grid."),
            _ => throw new DomainException($"Unsupported tactical area shape: {shape}."),
        };

        var cells = new List<GridPosition>();

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) > radius)
                    continue;

                var cell = new GridPosition(center.X + dx, center.Y + dy);
                if (battlefield.Contains(cell))
                    cells.Add(cell);
            }
        }

        return cells;
    }

    /// <summary>
    /// Une cible est-elle atteignable depuis <paramref name="origin"/> ?
    /// </summary>
    /// <param name="requiresLineOfSight">
    /// Vrai pour une compétence à distance. Une frappe au contact n'a rien à voir : elle
    /// s'applique à bout portant, où la question de la vue ne se pose pas.
    /// </param>
    public static bool IsInRange(
        TacticalBattlefield battlefield,
        GridPosition origin,
        GridPosition target,
        int range,
        bool requiresLineOfSight)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        // A single step of elevation (up OR down) is the everyday case — two adjacent tiles a
        // stair apart must stay attackable at melee range, or the most ordinary terrain on the
        // board would make basic attacks miss for a reason no player can see on screen. Only
        // elevation beyond that first step — a real cliff, two steps or more — actually eats
        // into range, same idea as the extra reach it takes to close real distance.
        var elevationDifference = Math.Abs(
            battlefield.ElevationAt(origin) - battlefield.ElevationAt(target));
        var elevationPenalty = Math.Max(0, elevationDifference - 1);
        if (origin.ManhattanDistanceTo(target) + elevationPenalty > range)
            return false;

        // A higher attacker has plunging sight over intervening obstacles.
        if (battlefield.ElevationAt(origin) > battlefield.ElevationAt(target))
            return true;

        return !requiresLineOfSight
            || TacticalMovement.HasLineOfSight(battlefield, origin, target);
    }

    /// <summary>
    /// Les combattants réellement touchés par une compétence, une fois la zone posée.
    /// </summary>
    /// <param name="affectedCells">
    /// Les cases de la zone, ou <c>null</c> pour une compétence <see cref="TacticalAreaShape.Map"/>
    /// qui couvre le champ entier.
    /// </param>
    /// <param name="casterSide">
    /// Le camp du lanceur. Sert à écarter le tir ami : un sort ne blesse jamais son propre camp,
    /// <b>sauf</b> en portée carte, où la symétrie est le prix de la puissance.
    /// </param>
    /// <param name="hostileOnly">
    /// Vrai pour une compétence offensive. Une compétence bénéfique inverse simplement le filtre :
    /// elle ne touche que le camp du lanceur.
    /// </param>
    public static IReadOnlyCollection<Combatant> ResolveTargets(
        TacticalCombat combat,
        IReadOnlyCollection<GridPosition>? affectedCells,
        CombatantSide casterSide,
        bool hostileOnly,
        TacticalAreaShape shape)
    {
        ArgumentNullException.ThrowIfNull(combat);

        var everyone = combat.Allies.Concat(combat.Enemies).Where(c => !c.IsDefeated);

        // Portée carte : aucun filtre de camp, personne n'est épargné. C'est toute la mécanique.
        if (shape == TacticalAreaShape.Map)
            return everyone.ToList();

        if (affectedCells is null)
            throw new DomainException(
                "Affected cells are required for any skill that is not map-wide.");

        var cells = affectedCells.ToHashSet();

        return everyone
            .Where(c => cells.Contains(combat.PositionOf(c.Id.Value)))
            .Where(c => hostileOnly ? c.Side != casterSide : c.Side == casterSide)
            .ToList();
    }

    /// <summary>
    /// Returns the first living combatant crossed by a line-of-sight effect before
    /// its intended centre. Plunging sight bypasses interception.
    /// </summary>
    public static Combatant? FindInterceptor(
        TacticalCombat combat,
        GridPosition origin,
        GridPosition intendedCenter)
    {
        ArgumentNullException.ThrowIfNull(combat);

        if (combat.Battlefield.ElevationAt(origin)
            > combat.Battlefield.ElevationAt(intendedCenter))
        {
            return null;
        }

        var occupants = combat.Allies
            .Concat(combat.Enemies)
            .Where(c => !c.IsDefeated)
            .ToDictionary(c => combat.PositionOf(c.Id.Value));

        foreach (var cell in TacticalMovement.CellsOnLine(origin, intendedCenter))
        {
            if (cell == origin || cell == intendedCenter)
                continue;

            if (occupants.TryGetValue(cell, out var interceptor))
                return interceptor;
        }

        return null;
    }

    /// <summary>
    /// Traduit un mode de ciblage du catalogue en forme de zone tactique.
    /// </summary>
    /// <remarks>
    /// Correspondance volontairement conservatrice : les modes « tous » deviennent un losange, pas
    /// une portée carte. Une compétence qui doit vraiment balayer le champ entier devra le déclarer
    /// explicitement — l'y promouvoir par défaut transformerait chaque sort de groupe existant en
    /// arme à double tranchant, sans que personne l'ait décidé.
    /// </remarks>
    public static TacticalAreaShape ShapeForCatalogTargeting(string targetingType) =>
        targetingType switch
        {
            "Self" or "SingleAlly" or "SingleEnemy" => TacticalAreaShape.Single,
            "AllEnemies" or "AllAllies" => TacticalAreaShape.Diamond,
            _ => throw new DomainException($"Unsupported targeting type: {targetingType}."),
        };

    /// <summary>Un mode de ciblage vise-t-il le camp adverse ?</summary>
    public static bool IsHostile(string targetingType) =>
        targetingType switch
        {
            "SingleEnemy" or "AllEnemies" => true,
            "Self" or "SingleAlly" or "AllAllies" => false,
            _ => throw new DomainException($"Unsupported targeting type: {targetingType}."),
        };
}
