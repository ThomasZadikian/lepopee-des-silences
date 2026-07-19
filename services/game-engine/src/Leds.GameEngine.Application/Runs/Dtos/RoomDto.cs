using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

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
    string? LayoutTemplateKey,
    string? LayoutTemplateVersion,
    string? ActiveClimate = null,
    PalaceRoomStateDto? PalaceState = null,
    string? CatalogRoomKey = null,
    string? CatalogName = null,
    string? CatalogNarrative = null,
    RoomGridDto? Grid = null)
{
    public static RoomDto FromDomain(Room room, IReadOnlyCollection<RunModifier>? runModifiers = null)
    {
        var activeClimate = ResolveActiveClimate(room, runModifiers ?? []);

        // Tactical mode (Grid not null): only fog-of-war-revealed nodes are ever sent to the
        // client — sending the full node list would defeat the point of the fog of war.
        // Classic mode: unchanged, the full node list as always.
        var nodesForDto = room.Grid is not null ? room.VisibleNodes : room.Nodes;

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
            room.LayoutTemplateKey,
            room.LayoutTemplateVersion,
            activeClimate,
            PalaceRoomStateDto.FromDomain(room.PalaceState),
            room.CatalogBinding?.Key,
            room.CatalogBinding?.DisplayName,
            room.CatalogBinding?.NarrativeText,
            room.Grid is null ? null : RoomGridDto.FromDomain(room.Grid, room.CanChallengeBossRemotely));
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
            _ => null
        };
    }
}

/// <summary>Tactical-mode grid overlay — <c>null</c> for a Classic room.</summary>
public sealed record RoomGridDto(
    int Width,
    int Height,
    int MovementBudget,
    int MovementBudgetRemaining,
    int PartyX,
    int PartyY,
    bool CanChallengeBossRemotely,
    IReadOnlyCollection<int[]> RevealedCells)
{
    public static RoomGridDto FromDomain(RoomGrid grid, bool canChallengeBossRemotely)
    {
        return new RoomGridDto(
            grid.Width,
            grid.Height,
            grid.MovementBudget,
            grid.MovementBudgetRemaining,
            grid.PartyX,
            grid.PartyY,
            canChallengeBossRemotely,
            grid.RevealedCells.Select(cell => new[] { cell.X, cell.Y }).ToArray());
    }
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
