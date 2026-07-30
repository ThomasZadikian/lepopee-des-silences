using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Ce qu'un ennemi décide de faire à son tour : qui viser, puis où aller (cf. SFD v2, §13).
/// </summary>
/// <remarks>
/// <para>
/// L'ordre compte : <b>la cible d'abord, le déplacement ensuite</b>. Choisir d'abord où aller
/// produirait des créatures qui se repositionnent joliment sans jamais menacer personne. En
/// choisissant la proie puis le chemin, un ennemi hors de portée avance quand même — il ne gâche
/// jamais son tour à faire du surplace.
/// </para>
/// <para>
/// Le score de ciblage ne tient pas compte de la portée : une cible inatteignable ce tour-ci
/// reste la bonne cible, on met simplement un tour de plus à l'atteindre. La contrainte de portée
/// dans le choix lui-même est explicitement hors périmètre.
/// </para>
/// </remarks>
public static class TacticalEnemyAi
{
    /// <summary>Poids de la blessure : une proie déjà entamée attire. // BALANCE KNOB</summary>
    public const double WoundWeight = 0.6;

    /// <summary>Pénalité par case d'éloignement. Départage sans dominer. // BALANCE KNOB</summary>
    public const double DistancePenalty = 0.02;

    /// <summary>
    /// Au-delà de cette distance à son plus proche congénère, un ennemi se sent isolé et le
    /// déplacement en tient compte. // BALANCE KNOB
    /// </summary>
    public const int CohesionComfortRadius = 4;

    /// <summary>Poids de l'isolement dans le choix de la case. // BALANCE KNOB</summary>
    public const double CohesionWeight = 0.5;

    /// <summary>Seuil de PV pour déclencher la fuite (30%). // O-008</summary>
    private const double LowHpThreshold = 0.3;

    /// <summary>
    /// Ce que le rôle d'une cible la rend désirable. Les soutiens en tête : les laisser vivre,
    /// c'est laisser le reste du groupe se relever indéfiniment.
    /// </summary>
    /// <remarks>// BALANCE KNOB</remarks>
    private static double RoleAppetiteFor(string archetype) => archetype.ToLowerInvariant() switch
    {
        "support" => 0.35,
        "skirmisher" => 0.25,
        "bruiser" => 0.10,
        "guard" => 0.0,
        _ => 0.15,
    };

    /// <summary>
    /// La cible que <paramref name="actorId"/> juge la plus intéressante, ou <c>null</c> si plus
    /// personne n'est debout en face.
    /// </summary>
    public static Combatant? ChooseTarget(TacticalCombat combat, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(combat);

        if (!combat.Enemies.Any(e => e.Id.Value == actorId))
            throw new DomainException($"Combatant '{actorId}' is not an enemy of this combat.");

        var origin = combat.PositionOf(actorId);

        return combat.Allies
            .Where(a => !a.IsDefeated)
            .Select(a => new
            {
                Candidate = a,
                Score = ScoreTarget(a, origin.ManhattanDistanceTo(combat.PositionOf(a.Id.Value))),
            })
            .OrderByDescending(x => x.Score)
            // Départage stable : sans lui, deux cibles au même score dépendraient de l'ordre
            // d'énumération, et le même combat rejoué ne donnerait pas la même chose.
            .ThenBy(x => x.Candidate.DisplayName, StringComparer.Ordinal)
            .ThenBy(x => x.Candidate.Id.Value)
            .Select(x => x.Candidate)
            .FirstOrDefault();
    }

    private static double ScoreTarget(Combatant target, int distance)
    {
        var woundedness = target.MaxVitality <= 0
            ? 0
            : 1.0 - (target.CurrentVitality / (double)target.MaxVitality);

        return (woundedness * WoundWeight)
               + RoleAppetiteFor(target.Archetype)
               - (distance * DistancePenalty);
    }

    /// <summary>
    /// Où <paramref name="actorId"/> se poste pour s'en prendre à <paramref name="target"/>. 
    /// Renvoie sa position actuelle s'il n'a rien de mieux à faire que rester là.
    /// </summary>
    /// <remarks>
    /// Le choix arbitre entre serrer la cible et ne pas se détacher du groupe : un ennemi qui
    /// fonce seul dans la mêlée meurt isolé, ce qui rend les familles à soutien inopérantes —
    /// un soigneur ne soigne que ce qui reste à sa portée. D'où la pénalité d'isolement, qui ne
    /// se déclenche qu'au-delà d'un rayon de confort.
    /// 
    /// Modifié pour O-008 : fuir si PV bas.
    /// </remarks>
    public static GridPosition ChooseDestination(
        TacticalCombat combat, Guid actorId, Combatant target)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(target);

        var actor = combat.Enemies.FirstOrDefault(e => e.Id.Value == actorId)
            ?? throw new DomainException($"Combatant '{actorId}' is not an enemy of this combat.");

        var origin = combat.PositionOf(actorId);
        var targetCell = combat.PositionOf(target.Id.Value);

        var occupied = combat.OccupiedCells().ToHashSet();
        occupied.Remove(origin);
        var traversableAllies = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != actorId)
            .Select(e => combat.PositionOf(e.Id.Value))
            .ToHashSet();

        var reachable = TacticalMovement.ReachableCells(
            combat.Battlefield,
            origin,
            TacticalMovement.BudgetFor(actor.EffectiveMovement),
            occupied,
            traversableAllies);

        // Les congénères encore debout, hors soi-même : la référence de cohésion.
        var kin = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != actorId)
            .Select(e => combat.PositionOf(e.Id.Value))
            .ToList();

        // O-008: Fuir si PV bas
        var isLowHp = actor.CurrentVitality <= actor.MaxVitality * LowHpThreshold;
        var shouldFlee = isLowHp && kin.Count > 0; // Ne fuir que s'il y a des alliés pour se cacher derrière

        if (shouldFlee)
        {
            // Se déplacer loin de la cible ET vers les alliés
            return reachable.Keys
                .OrderBy(cell => FleeCostOf(cell, targetCell, kin))
                .ThenBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .First();
        }

        // Comportement normal : se rapprocher de la cible
        return reachable.Keys
            .OrderBy(cell => CostOf(cell, targetCell, kin))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .First();
    }

    private static double CostOf(
        GridPosition cell, GridPosition targetCell, IReadOnlyCollection<GridPosition> kin)
    {
        var toTarget = cell.ManhattanDistanceTo(targetCell);

        if (kin.Count == 0)
            return toTarget;

        var solitude = kin.Min(k => cell.ManhattanDistanceTo(k));
        var isolation = Math.Max(0, solitude - CohesionComfortRadius);

        return toTarget + (isolation * CohesionWeight);
    }

    /// <summary>
    /// Coût pour fuir : maximiser la distance à la cible et minimiser l'isolement.
    /// </summary>
    private static double FleeCostOf(
        GridPosition cell, GridPosition targetCell, IReadOnlyCollection<GridPosition> kin)
    {
        var fromTarget = cell.ManhattanDistanceTo(targetCell);
        var toKin = kin.Count > 0 ? kin.Min(k => cell.ManhattanDistanceTo(k)) : 0;

        // Privilégier les cases éloignées de la cible et proches des alliés
        // Note: on inverse fromTarget pour fuir (plus la distance est grande, plus le coût est bas)
        return -fromTarget + (toKin * 0.5);
    }
}
