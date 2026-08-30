using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunGenerator
{
    string GeneratorVersion { get; }

    string MarkovMatrixVersion { get; }

    string GenerateSeed();

    Task<Room> GenerateInitialRoomAsync(
        string seed,
        CancellationToken cancellationToken = default);

    Task<Room> GenerateNextRoomAsync(Run run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the room for one specific, already-chosen catalog destination — the
    /// confirmed room-exit path (see Run.ConfirmRoomExit). Unlike
    /// <see cref="GenerateNextRoomAsync"/>, this never rolls a destination when one is given:
    /// the exit the player confirmed already fixed it when the CURRENT room was generated
    /// (see MapNode.ExitDestinationRoomKey), so this only materializes the grid/nodes for it.
    /// Null <paramref name="destination"/> means a legacy Exit (no reachability graph at
    /// placement time) — the one case that still rolls, via the old per-theme weighted path.
    /// </summary>
    Task<Room> GenerateSpecificRoomAsync(
        Run run,
        CatalogRoomDefinition? destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// "Édit des Portes Ouvertes" (law.portes-ouvertes): previews the catalog identity
    /// (key/display name) of every remaining room in the run's current floor, without
    /// generating or persisting anything. The room *type/identity* sequence is fully
    /// deterministic from (seed, catalog room graph, visited room keys) — unlike a room's
    /// internal grid/nodes, which stay procedurally generated only once actually entered —
    /// so this is a real forecast, not a guess. A null <see cref="UpcomingRoomPreview.Key"/>
    /// means that room slot would fall back to the unbound procedural scaffold (no catalog
    /// room matched its theme/depth window).
    /// </summary>
    Task<IReadOnlyList<UpcomingRoomPreview>> PreviewUpcomingRoomNamesAsync(
        Run run,
        CancellationToken cancellationToken = default);
}

/// <summary>One forecasted room slot for the rest of the current floor.</summary>
public sealed record UpcomingRoomPreview(int RoomIndex, string? Key, string? DisplayName);