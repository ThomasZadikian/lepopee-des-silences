using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>Un combattant tactique : son état, plus la case qu'il tient.</summary>
public sealed record TacticalCombatantRuntimeDto(
    CombatantRuntimeDto Combatant,
    int X,
    int Y,
    bool HasMoved,
    bool HasActed,
    int MovementBudget,
    string Facing,
    IReadOnlyDictionary<string, int> SkillCooldowns);

/// <summary>
/// Le terrain une fois la salle vidée de ses nœuds : reliefs et trous, rien d'autre.
/// </summary>
/// <remarks>
/// <c>Walkable</c> et <c>Elevation</c> sont plats et rangés en row-major (<c>y * Width + x</c>),
/// même convention que <c>RoomGridDto</c> côté exploration — le client sait déjà les lire.
/// </remarks>
public sealed record TacticalBattlefieldDto(
    int Width,
    int Height,
    IReadOnlyList<int> Elevation,
    IReadOnlyList<bool> Walkable,
    /// <summary>
    /// La case appartient-elle à la salle ? Distinct de <c>Walkable</c> : une case du vide et
    /// une case encombrée sont toutes deux infranchissables, mais seule la seconde porte un
    /// obstacle à peindre.
    /// </summary>
    IReadOnlyList<bool> Floor);

/// <summary>
/// L'état d'un combat tactique tel que le client doit l'afficher.
/// </summary>
/// <remarks>
/// Expose le terrain, les positions et l'ordre d'initiative annoncé à l'avance.
/// </remarks>
public sealed record TacticalCombatRuntimeDto(
    Guid Id,
    string Status,
    int RoundNumber,
    Guid? ActiveCombatantId,
    IReadOnlyList<Guid> InitiativeOrder,
    TacticalBattlefieldDto Battlefield,
    IReadOnlyCollection<TacticalCombatantRuntimeDto> Allies,
    IReadOnlyCollection<TacticalCombatantRuntimeDto> Enemies,
    IReadOnlyCollection<CombatUsableItemDto> UsableBattleItems,
    IReadOnlyCollection<string> UsedOnceSkillKeys,
    TacticalEscapeDto? Escape,
    string RiskTier)
{
    public static TacticalCombatRuntimeDto FromDomain(
        TacticalCombat combat,
        IReadOnlyCollection<CombatUsableItemDto>? usableItems = null)
    {
        ArgumentNullException.ThrowIfNull(combat);

        var field = combat.Battlefield;
        var elevation = new int[field.Width * field.Height];
        var walkable = new bool[field.Width * field.Height];
        var floor = new bool[field.Width * field.Height];

        for (var y = 0; y < field.Height; y++)
        {
            for (var x = 0; x < field.Width; x++)
            {
                var cell = new GridPosition(x, y);
                var index = (y * field.Width) + x;
                walkable[index] = field.IsWalkable(cell);
                floor[index] = field.IsFloor(cell);
                // Toute case de la salle porte son relief, obstacle compris : un éboulis se
                // peint sur la tuile qui le supporte, pas au niveau zéro.
                elevation[index] = floor[index] ? field.ElevationAt(cell) : 0;
            }
        }

        return new TacticalCombatRuntimeDto(
            Id: combat.Id.Value,
            Status: combat.Status.ToString(),
            RoundNumber: combat.RoundNumber,
            ActiveCombatantId: combat.ActiveCombatantId,
            InitiativeOrder: combat.InitiativeOrder,
            Battlefield: new TacticalBattlefieldDto(
                field.Width, field.Height, elevation, walkable, floor),
            Allies: [.. combat.Allies.Select(c => Project(combat, c))],
            Enemies: [.. combat.Enemies.Select(c => Project(combat, c))],
            UsableBattleItems: usableItems ?? [],
            UsedOnceSkillKeys: [.. combat.UsedOnceSkillKeys],
            Escape: combat.EscapePosition is { } exit
                ? new TacticalEscapeDto(exit.X, exit.Y)
                : null,
            RiskTier: combat.RiskTier.ToString());
    }

    private static TacticalCombatantRuntimeDto Project(
        TacticalCombat combat, Domain.Combats.Combatant combatant)
    {
        var position = combat.PositionOf(combatant.Id.Value);
        var turn = combat.TurnStateOf(combatant.Id.Value);

        return new TacticalCombatantRuntimeDto(
            // Même bestiaire, mêmes stats et mêmes statuts, enrichis du placement tactique.
            Combatant: CombatantRuntimeDto.FromDomain(
                combatant,
                combat.CurrentTick,
                combat.EmotionalAffinityMatrix),
            X: position.X,
            Y: position.Y,
            HasMoved: turn.HasMoved,
            HasActed: turn.HasActed,
            MovementBudget: TacticalMovement.BudgetFor(combatant.HasTacticalSlow
                ? Math.Max(1, combatant.EffectiveMovement / 2)
                : combatant.EffectiveMovement),
            Facing: combat.FacingOf(combatant.Id.Value).ToString(),
            SkillCooldowns: combat.CooldownsOf(combatant.Id.Value));
    }
}

public sealed record TacticalEscapeDto(int X, int Y);
