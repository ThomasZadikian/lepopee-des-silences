using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

/// <param name="Grid">
/// Always populated for a live domain Room — kept as a nullable optional parameter only for
/// positional-record convenience, not because a room can lack a grid.
/// </param>
public sealed record RoomDto(
    Guid Id,
    int Depth,
    string RoomType,
    string Theme,
    string State,
    int CurrentNodeDepth,
    int MaxNodeDepth,
    int TotalNodeCount,
    RoomBossProfileDto BossPreview,
    IReadOnlyCollection<MapNodeDto> Nodes,
    IReadOnlyCollection<MapNodeDto> AvailableNodes,
    IReadOnlyCollection<RoomNpcDto> RoomNpcs,
    string? LayoutTemplateKey,
    string? LayoutTemplateVersion,
    string? ActiveClimate = null,
    PalaceRoomStateDto? PalaceState = null,
    string? CatalogRoomKey = null,
    string? CatalogName = null,
    string? CatalogNarrative = null,
    RoomGridDto? Grid = null)
{
    public static RoomDto FromDomain(
        Room room,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        IReadOnlyCollection<RunItem>? groundItems = null)
    {
        var activeClimate = ResolveActiveClimate(room, runModifiers ?? []);

        // Fog-of-war-revealed nodes, PLUS every still-Available node regardless of reveal
        // state — so the player always knows which directions hold an objective, even
        // unexplored. This only exposes a node's type/position as a distant marker; the
        // terrain/path to it stays hidden (RevealedCells is untouched). Sending the full node
        // list unconditionally would defeat the point of the fog of war entirely.
        //
        // Hidden nodes are excluded from BOTH halves: a cache the player is meant to search for
        // must not show up as a distant marker, which the Available branch would otherwise do.
        var nodesForDto = room.VisibleNodes
            .Concat(room.Nodes.Where(node => node.State == NodeState.Available && !node.IsHidden))
            .Distinct()
            .ToArray();

        // Same fog-of-war rationale as nodesForDto above: a PNJ standing in an unexplored cell
        // must not leak its position through the DTO before the party has actually seen it.
        var revealedCells = new HashSet<(int X, int Y)>(room.Grid.RevealedCells);
        var roomNpcsForDto = room.RoomNpcs
            .Where(npc => revealedCells.Contains((npc.X, npc.Y)))
            .ToArray();

        return new RoomDto(
            room.Id.Value,
            room.Depth,
            room.RoomType.ToString(),
            room.Theme,
            room.State.ToString(),
            room.CurrentNodeDepth,
            room.MaxNodeDepth,
            room.TotalNodeCount,
            RoomBossProfileDto.FromDomain(room.BossProfile),
            nodesForDto.Select(MapNodeDto.FromDomain).ToArray(),
            room.AvailableNodes.Select(MapNodeDto.FromDomain).ToArray(),
            roomNpcsForDto.Select(RoomNpcDto.FromDomain).ToArray(),
            room.LayoutTemplateKey,
            room.LayoutTemplateVersion,
            activeClimate,
            PalaceRoomStateDto.FromDomain(room.PalaceState),
            room.CatalogBinding?.Key,
            room.CatalogBinding?.DisplayName,
            room.CatalogBinding?.NarrativeText,
            RoomGridDto.FromDomain(
                room.Grid,
                room.CanChallengeBossRemotely,
                room.CanSearchAtPartyPosition,
                room.HintCells,
                (groundItems ?? [])
                    .Where(item => item.GroundRoomId == room.Id.Value)
                    .ToArray()));
    }

    private static string? ResolveActiveClimate(
        Room room,
        IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == room.Id.Value)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => "Grey",
            2 => "Rain",
            3 => "Heatwave",
            4 => "Hail",
            5 => "Brume",
            6 => "Orage",
            7 => "PluieDeCendres",
            8 => "PluieViolacee",
            9 => "Accalmie",
            _ => null
        };
    }
}

/// <summary>Free-movement grid overlay.</summary>
public sealed record RoomGridDto(
    int Width,
    int Height,
    int MovementBudget,
    int MovementBudgetRemaining,
    int PartyX,
    int PartyY,
    bool CanChallengeBossRemotely,
    IReadOnlyCollection<int[]> RevealedCells,
    // Flat, row-major (index = y*Width+x), one value 0..3 per cell — sent for the whole grid,
    // not gated by fog of war, so the renderer can autotile terrain at the fog boundary and
    // objective markers can still show a sensible tile through unexplored fog.
    IReadOnlyList<int> Elevation,
    // [x,y] pairs, same shape convention as RevealedCells — sent for the whole grid, same
    // rationale as Elevation above.
    IReadOnlyCollection<int[]> ObstacleCells,
    // Flat, row-major like Elevation: which cells are part of the room at all. False = a hole
    // in the bounding rectangle, which is what gives a room a shape (alcoves, an L, ragged
    // edges) and tells the renderer where to draw cliff faces.
    IReadOnlyList<bool> FloorCells,
    // [x,y] pairs, same shape convention as RevealedCells — the threshold of a sub-room
    // (PartitionSubRooms). Never a wall, never gated by fog of war (same rationale as
    // Elevation/FloorCells above). Belongs to no enceinte: the client's region flood-fill
    // (Chantier 5) treats a door cell as a boundary rather than assigning it to either side.
    IReadOnlyCollection<int[]> DoorCells,
    // Whether searching from where the party stands would turn something up.
    bool CanSearch,
    // [x,y] of every cell holding an unfound cache. POSITION ONLY — no id, type or reward: the
    // player is meant to see that a slab rings hollow and choose whether to spend budget
    // finding out what is under it. Withholding the position entirely (as this DTO first did)
    // left nothing to notice, so nothing could motivate a search.
    IReadOnlyCollection<int[]> HintCells,
    IReadOnlyCollection<GroundItemDto> GroundItems,
    // Authored per-cell floor-material overrides (e.g. the Hall's tapis band) — never gated by
    // fog of war, same rationale as Elevation/FloorCells above. Empty for a procedurally-
    // generated room, which paints its whole floor from one per-room surface instead.
    IReadOnlyCollection<CellDecorDto> SurfaceOverrides,
    // Authored per-cell decor placements (e.g. the Hall's four marble pillars) — purely a
    // rendering hint, independent from ObstacleCells (see RoomGrid.DecorPlacements's own remarks
    // on why decor and collision are separate).
    IReadOnlyCollection<CellDecorDto> DecorPlacements)
{
    public static RoomGridDto FromDomain(
        RoomGrid grid,
        bool canChallengeBossRemotely,
        bool canSearch,
        IReadOnlyCollection<(int X, int Y)> hintCells,
        IReadOnlyCollection<RunItem> groundItems)
    {
        return new RoomGridDto(
            grid.Width,
            grid.Height,
            grid.MovementBudget,
            grid.MovementBudgetRemaining,
            grid.PartyX,
            grid.PartyY,
            canChallengeBossRemotely,
            grid.RevealedCells.Select(cell => new[] { cell.X, cell.Y }).ToArray(),
            grid.Elevation.ToArray(),
            grid.Obstacles.Select(cell => new[] { cell.X, cell.Y }).ToArray(),
            grid.FloorMask.ToArray(),
            grid.Doors.Select(cell => new[] { cell.X, cell.Y }).ToArray(),
            canSearch,
            hintCells.Select(cell => new[] { cell.X, cell.Y }).ToArray(),
            groundItems.Select(GroundItemDto.FromDomain).ToArray(),
            grid.SurfaceOverrides.Select(kv => new CellDecorDto(kv.Key.X, kv.Key.Y, kv.Value)).ToArray(),
            grid.DecorPlacements.Select(kv => new CellDecorDto(kv.Key.X, kv.Key.Y, kv.Value)).ToArray());
    }
}

public sealed record CellDecorDto(int X, int Y, string Key);

public sealed record GroundItemDto(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    string Rarity,
    int Quantity,
    int X,
    int Y)
{
    public static GroundItemDto FromDomain(RunItem item) => new(
        item.Id.Value,
        item.DefinitionKey,
        item.DisplayName,
        item.Rarity.ToString(),
        item.Quantity,
        item.GroundX!.Value,
        item.GroundY!.Value);
}

public sealed record RoomNpcDto(
    Guid Id,
    string CatalogNpcKey,
    int X,
    int Y,
    string Behavior,
    string Awareness)
{
    public static RoomNpcDto FromDomain(RoomNpc npc) => new(
        npc.Id.Value,
        npc.CatalogNpcKey,
        npc.X,
        npc.Y,
        npc.Behavior.ToString(),
        npc.Awareness.ToString());
}

public sealed record PalaceRoomStateDto(
    string Key,
    string Label,
    string Description,
    string Tone)
{
    public static PalaceRoomStateDto FromDomain(PalaceRoomState state)
    {
        return state switch
        {
            PalaceRoomState.Silent => new(
                "silent",
                "Silencieuse",
                "La salle retient son souffle et pousse les ennemis a se retrancher.",
                "silent"),
            PalaceRoomState.Painful => new(
                "painful",
                "Douloureuse",
                "La salle transforme la violence directe en douleur persistante.",
                "painful"),
            PalaceRoomState.Enraged => new(
                "enraged",
                "Enragee",
                "La salle nourrit une agressivite instable.",
                "danger"),
            PalaceRoomState.Violent => new(
                "violent",
                "Violente",
                "La salle favorise les affrontements brutaux.",
                "danger"),
            _ => new(
                "neutral",
                "Neutre",
                "La salle ne manifeste pas d'etat adaptatif notable.",
                "neutral")
        };
    }
}
