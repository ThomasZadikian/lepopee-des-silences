using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Reachability;
using Leds.GameEngine.Infrastructure.Generation.Rooms.States;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    /// <summary>
    /// Shown to the player when the very first room could not be bound to a real catalog
    /// room (no "Palais" World configured, or its entry room unresolved) — the run still
    /// starts on the procedural Threshold scaffold, but none of the Bestiaire families are
    /// authored against that legacy room type, so encounters there fall back to the old
    /// pre-Bestiaire placeholder roster. Surfaced narratively rather than failing the run,
    /// since the legacy scaffold is still fully playable.
    /// </summary>
    internal const string StructurelessPalaceNotice =
        "Le Palais n'a pas sa structure habituelle, tout semble... sans vie.";

    private readonly ISeededRandomFactory _randomFactory;
    private readonly ICatalogRoomTypeResolver _catalogRoomTypeResolver;
    private readonly IRoomReachabilitySelector _roomReachabilitySelector;
    private readonly IPalaceRoomStateResolver _palaceRoomStateResolver;
    private readonly IGridRoomGenerator _gridRoomGenerator;
    private readonly IRunPsycheEvolver _psycheEvolver;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public DeterministicRunGenerator(
        ISeededRandomFactory randomFactory,
        ICatalogRoomTypeResolver catalogRoomTypeResolver,
        IRoomReachabilitySelector roomReachabilitySelector,
        IPalaceRoomStateResolver palaceRoomStateResolver,
        IGridRoomGenerator gridRoomGenerator,
        IRunPsycheEvolver psycheEvolver,
        ICatalogContentGateway catalogContentGateway)
    {
        _randomFactory = randomFactory;
        _catalogRoomTypeResolver = catalogRoomTypeResolver;
        _roomReachabilitySelector = roomReachabilitySelector;
        _palaceRoomStateResolver = palaceRoomStateResolver;
        _gridRoomGenerator = gridRoomGenerator;
        _psycheEvolver = psycheEvolver;
        _catalogContentGateway = catalogContentGateway;
    }

    public string GeneratorVersion => DefaultGridRoomLayoutTemplates.GeneratorVersion;

    public string MarkovMatrixVersion => StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

    public string GenerateSeed()
    {
        return $"seed-{Guid.NewGuid():N}";
    }

    public async Task<Room> GenerateInitialRoomAsync(
        string seed,
        CancellationToken cancellationToken = default)
    {
        var random = _randomFactory.CreateForRoom(
            seed,
            roomDepth: 0,
            GeneratorVersion);

        // Refonte des Rooms : si un Monde est configuré au catalogue (ex. "Palais"), un
        // nouveau run démarre directement sur sa salle de niveau 0. Un seul Monde existe
        // pour la bêta ; l'ordre alphabétique des clés sert de désambiguïsation stable en
        // attendant qu'un vrai concept de sélection de monde de départ existe.
        var worlds = await _catalogContentGateway.ListWorldDefinitionsAsync(cancellationToken);
        var startingWorld = worlds.OrderBy(w => w.Key, StringComparer.Ordinal).FirstOrDefault();

        if (startingWorld is not null)
        {
            var definitions = await _catalogContentGateway.ListRoomDefinitionsAsync(cancellationToken);
            var entryRoom = definitions.FirstOrDefault(d =>
                string.Equals(d.Key, startingWorld.EntryRoomKey, StringComparison.OrdinalIgnoreCase));

            if (entryRoom is not null)
            {
                var entryRoomType = MapThemeToScaffold(entryRoom.Theme);
                var entryScaffold = await GenerateRoomShapeAsync(
                    seed, GeneratorVersion, roomDepth: 0, entryRoomType, random, cancellationToken,
                    PalaceRoomState.Neutral, entryRoom.Key, entryRoom.BossDefinitionKey);
                AttachCatalogRoom(entryScaffold, entryRoom);
                await AttachExitPlacementAsync(entryScaffold, seed, roomDepth: 0, cancellationToken);
                return entryScaffold;
            }
        }

        // Legacy path (SAL-2/SAL-4): no World configured, or its entry room can't be
        // resolved in the catalog — start on the procedural Threshold scaffold, unchanged.
        var room = await GenerateRoomShapeAsync(
                    seed,
                    GeneratorVersion,
                    roomDepth: 0,
                    roomType: RoomType.Threshold,
                    random,
                    cancellationToken,
                    PalaceRoomState.Neutral);

        await AttachCatalogRoomAsync(room, CatalogMarkovRoomTypeResolver.ThresholdTheme, seed, roomDepth: 0, cancellationToken);

        if (room.CatalogBinding is null)
        {
            room.AttachCatalogBinding(new CatalogRoomBinding(
                Key: "system.fallback.threshold",
                DisplayName: string.Empty,
                NarrativeText: StructurelessPalaceNotice,
                EnemyPoolKey: null,
                RewardPoolKey: null,
                LawPoolKey: null,
                CursePoolKey: null,
                IsUnique: false));
        }

        await AttachExitPlacementAsync(room, seed, roomDepth: 0, cancellationToken);

        return room;
    }

    public async Task<Room> GenerateNextRoomAsync(
        Run run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var nextRoomDepth = run.CurrentDepth + 1;
        var (roomType, themeKey, preResolvedDefinition) = await ResolveNextRoomAsync(run, nextRoomDepth, cancellationToken);
        var room = await GenerateRoomShapeForDepthAsync(
            run, roomType, nextRoomDepth, cancellationToken, preResolvedDefinition?.Key,
            preResolvedDefinition?.BossDefinitionKey);

        if (preResolvedDefinition is not null)
        {
            AttachCatalogRoom(room, preResolvedDefinition);
        }
        else
        {
            await AttachCatalogRoomAsync(room, themeKey, run.Seed, nextRoomDepth, cancellationToken);
        }

        await AttachExitPlacementAsync(room, run.Seed, nextRoomDepth, cancellationToken);

        return room;
    }

    /// <summary>
    /// Generates the room for one specific, already-chosen catalog destination — the
    /// confirmed room-exit path (see Run.ConfirmRoomExit). Never rolls a destination when one
    /// is given: the exit the player confirmed already fixed it when the CURRENT room was
    /// generated (see AttachExitPlacementAsync), so this only materializes the grid/nodes for
    /// it. <paramref name="destination"/> is null for a legacy Exit (no reachability graph at
    /// placement time, see MapNode.ExitDestinationRoomKey) — the only case that still rolls,
    /// via the same per-theme weighted path <see cref="GenerateNextRoomAsync"/> always used.
    /// </summary>
    public async Task<Room> GenerateSpecificRoomAsync(
        Run run,
        CatalogRoomDefinition? destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var nextRoomDepth = run.CurrentDepth + 1;
        RoomType roomType;
        string? legacyThemeKey = null;

        if (destination is not null)
        {
            roomType = MapThemeToScaffold(destination.Theme);
        }
        else
        {
            (roomType, legacyThemeKey, _) = await ResolveLegacyThemeRoomAsync(run, nextRoomDepth, cancellationToken);
        }

        var room = await GenerateRoomShapeForDepthAsync(
            run, roomType, nextRoomDepth, cancellationToken, destination?.Key,
            destination?.BossDefinitionKey);

        if (destination is not null)
        {
            AttachCatalogRoom(room, destination);
        }
        else
        {
            await AttachCatalogRoomAsync(room, legacyThemeKey!, run.Seed, nextRoomDepth, cancellationToken);
        }

        await AttachExitPlacementAsync(room, run.Seed, nextRoomDepth, cancellationToken);

        return room;
    }

    /// <summary>
    /// The shape-generation steps shared by <see cref="GenerateNextRoomAsync"/> and
    /// <see cref="GenerateSpecificRoomAsync"/> — Palace-state resolution and grid/node
    /// generation — factored out since only how <paramref name="roomType"/> gets resolved
    /// differs between the two callers (weighted roll vs. an already-chosen destination).
    /// </summary>
    private async Task<Room> GenerateRoomShapeForDepthAsync(
        Run run, RoomType roomType, int nextRoomDepth, CancellationToken cancellationToken,
        string? catalogRoomKey = null,
        string? bossDefinitionKey = null)
    {
        var matrixVersion = string.IsNullOrWhiteSpace(run.MarkovMatrixVersion)
            ? MarkovMatrixVersion
            : run.MarkovMatrixVersion;

        // Inconscient du Palais : distribution latente dérivée de l'historique de salles
        // (déterministe). Persiste (Advance), accumule (nudge) et biaise la génération.
        var psyche = _psycheEvolver.Evolve(run);

        var palaceState = _palaceRoomStateResolver.ResolveNextState(
            new PalaceRoomStateResolutionContext(
                Seed: run.Seed,
                MatrixVersion: matrixVersion,
                PreviousRoomState: run.CurrentRoom.PalaceState,
                PreviousRoomType: run.CurrentRoom.RoomType,
                NextRoomType: roomType,
                NextRoomDepth: nextRoomDepth,
                ActiveLawKeys: run.ActivePalaceLaws
                    .Where(law => !law.IsConsumed)
                    .Select(law => law.Key)
                    .ToArray(),
                ActiveCurseKeys: run.ActiveCurse is { IsConsumed: false } activeCurse
                    ? [activeCurse.Key]
                    : [],
                ActiveClimate: ResolveActiveClimate(run),
                Psyche: psyche));

        var random = _randomFactory.CreateForRoom(
            run.Seed,
            nextRoomDepth,
            GeneratorVersion);

        return await GenerateRoomShapeAsync(
            run.Seed,
            GeneratorVersion,
            nextRoomDepth,
            roomType,
            random,
            cancellationToken,
            palaceState,
            catalogRoomKey,
            bossDefinitionKey);
    }

    /// <summary>
    /// Fixes this room's exits the moment it's generated — one per catalog room it can reach
    /// at the next depth (see MapNode.ExitDestinationRoomKey), so every real branch is visible
    /// to the player instead of a silent weighted pick ("les enchaînements doivent être fixés
    /// au chargement"). Only resolves cheap catalog identities here — the destination Rooms
    /// themselves stay unmaterialized until the player actually confirms one (see
    /// GenerateSpecificRoomAsync), never calls GridRoomGenerator again.
    /// </summary>
    private async Task AttachExitPlacementAsync(
        Room room, string seed, int roomDepth, CancellationToken cancellationToken)
    {
        var nextRoomDepth = roomDepth + 1;
        var currentBinding = room.CatalogBinding;
        IReadOnlyCollection<CatalogRoomDefinition> eligible = [];

        if (currentBinding is not null)
        {
            var definitions = await _catalogContentGateway.ListRoomDefinitionsAsync(cancellationToken);
            var currentDefinition = definitions.FirstOrDefault(d =>
                string.Equals(d.Key, currentBinding.Key, StringComparison.OrdinalIgnoreCase));

            if (currentDefinition is not null && !string.IsNullOrWhiteSpace(currentDefinition.WorldKey))
            {
                var worlds = await _catalogContentGateway.ListWorldDefinitionsAsync(cancellationToken);
                eligible = _roomReachabilitySelector.SelectEligibleRooms(
                    currentDefinition, definitions, worlds, nextRoomDepth);
            }
        }

        var exitNodes = new List<MapNode>();

        if (eligible.Count == 0)
        {
            // No reachability graph for this room (legacy content with no WorldKey, or a
            // broken catalog reference) — a single exit whose destination is resolved via
            // the old per-theme weighted roll at confirmation time, same fallback
            // ResolveNextRoomAsync already used before this room had visible exits.
            var legacyCells = PickExitCells(room, seed, roomDepth, count: 1);

            if (legacyCells.Count > 0)
            {
                exitNodes.Add(CreateExitNode(legacyCells[0], destinationKey: null, destinationDisplayName: "???"));
            }
        }
        else
        {
            var cells = PickExitCells(room, seed, roomDepth, eligible.Count);
            exitNodes.AddRange(eligible.Zip(cells, (definition, cell) =>
                CreateExitNode(cell, definition.Key, definition.DisplayName)));
        }

        if (exitNodes.Count > 0)
        {
            room.AttachExitNodes(exitNodes);
        }
    }

    private static MapNode CreateExitNode(
        (int X, int Y) cell, string? destinationKey, string? destinationDisplayName)
    {
        return MapNode.Create(
            eventType: NodeEventType.Exit,
            riskLevel: 0,
            rewardProfile: "exit",
            row: cell.Y,
            lane: cell.X,
            parentNodeIds: Array.Empty<NodeId>(),
            exitDestinationRoomKey: destinationKey,
            exitDestinationDisplayName: destinationDisplayName);
    }

    /// <summary>
    /// Candidate cells for exit placement: the room's edge, walkable, not already taken by
    /// another node. Shuffled by a dedicated RNG (its own generator-version discriminator) so
    /// exit placement never perturbs the room shape's own floor/elevation/obstacle/node roll
    /// sequence — deterministic given the same seed, independent of it.
    /// </summary>
    private IReadOnlyList<(int X, int Y)> PickExitCells(Room room, string seed, int roomDepth, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var grid = room.Grid;
        var occupied = new HashSet<(int X, int Y)>(room.Nodes.Select(n => (n.Lane, n.Row)));

        var candidates = GridRoomGenerator.ComputeEdgeCells(grid.Width, grid.Height, grid.StartX, grid.StartY)
            .Where(cell => grid.IsWalkable(cell.X, cell.Y) && !occupied.Contains(cell))
            .ToList();

        var exitRandom = _randomFactory.CreateForRoom(seed, roomDepth, GeneratorVersion + ":exits");

        return candidates.OrderBy(_ => exitRandom.Next()).Take(count).ToArray();
    }

    /// <summary>
    /// "Édit des Portes Ouvertes" (law.portes-ouvertes). Replays the exact same room-identity
    /// resolution chain as <see cref="ResolveNextRoomAsync"/> (reachability graph, then the
    /// legacy per-theme fallback), one depth at a time for the rest of the current floor,
    /// without generating room shapes or touching persistence. Since neither path depends on
    /// anything but (seed, catalog room graph, visited room keys so far), chaining a locally
    /// simulated "visited keys" list forward reproduces exactly what real play will resolve —
    /// this is a genuine forecast, not an approximation.
    /// </summary>
    public async Task<IReadOnlyList<UpcomingRoomPreview>> PreviewUpcomingRoomNamesAsync(
        Run run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var roomsRemainingInFloor = Run.FloorLengthInRooms - (run.CurrentRoomIndex % Run.FloorLengthInRooms) - 1;
        if (roomsRemainingInFloor <= 0)
        {
            return [];
        }

        var definitions = await _catalogContentGateway.ListRoomDefinitionsAsync(cancellationToken);
        var worlds = await _catalogContentGateway.ListWorldDefinitionsAsync(cancellationToken);
        var themeAffinities = await _catalogContentGateway.ListRoomThemeAffinitiesAsync(cancellationToken);

        var visitedKeys = run.Rooms
            .Select(r => r.CatalogBinding?.Key)
            .Where(key => key is not null)
            .Select(key => key!)
            .ToList();

        var currentDefinition = run.CurrentRoom.CatalogBinding is { } currentBinding
            ? definitions.FirstOrDefault(d => string.Equals(d.Key, currentBinding.Key, StringComparison.OrdinalIgnoreCase))
            : null;
        var currentRoomType = run.CurrentRoom.RoomType;

        var results = new List<UpcomingRoomPreview>(roomsRemainingInFloor);

        for (var i = 0; i < roomsRemainingInFloor; i++)
        {
            var depth = run.CurrentRoomIndex + 1 + i;

            var selected = currentDefinition is not null && !string.IsNullOrWhiteSpace(currentDefinition.WorldKey)
                ? _roomReachabilitySelector.SelectNextRoom(
                    currentDefinition, definitions, worlds, themeAffinities, depth, visitedKeys, run.Seed)
                : null;

            if (selected is null)
            {
                var themeKey = await _catalogRoomTypeResolver.ResolveNextRoomTypeKeyAsync(
                    run.Seed, depth, currentRoomType.ToString(), cancellationToken);

                selected = SelectRoomDefinition(definitions, themeKey, depth, run.Seed);
                currentRoomType = MapThemeToScaffold(themeKey);
            }

            if (selected is null)
            {
                // No catalog room matched — that room slot stays purely procedural, with
                // no name to reveal (mirrors AttachCatalogRoomAsync's no-op when unmatched).
                results.Add(new UpcomingRoomPreview(depth, Key: null, DisplayName: null));
                currentDefinition = null;
                continue;
            }

            results.Add(new UpcomingRoomPreview(depth, selected.Key, selected.DisplayName));
            visitedKeys.Add(selected.Key);
            currentDefinition = selected;
        }

        return results;
    }

    /// <summary>
    /// Refonte des Rooms (SFD § 5) : quand la salle courante est liée à une RoomDefinition
    /// appartenant à un Monde, la salle suivante vient de son graphe de réachabilité
    /// explicite plutôt que du tirage par thème. Le contenu sans Monde assigné (ex. les
    /// salles canon Pittsburgh / L'épopée des Échos, hors périmètre bêta) continue d'utiliser
    /// exactement le chemin historique (SAL-4) — aucun changement de comportement pour lui.
    /// </summary>
    private async Task<(RoomType RoomType, string ThemeKey, CatalogRoomDefinition? PreResolvedDefinition)> ResolveNextRoomAsync(
        Run run, int nextRoomDepth, CancellationToken cancellationToken)
    {
        var currentBinding = run.CurrentRoom.CatalogBinding;
        if (currentBinding is not null)
        {
            // The three catalog reads are independent of one another — fetched
            // concurrently instead of one-after-another to keep per-room generation
            // latency down to a single round-trip instead of three.
            var definitionsTask = _catalogContentGateway.ListRoomDefinitionsAsync(cancellationToken);
            var worldsTask = _catalogContentGateway.ListWorldDefinitionsAsync(cancellationToken);
            var themeAffinitiesTask = _catalogContentGateway.ListRoomThemeAffinitiesAsync(cancellationToken);
            await Task.WhenAll(definitionsTask, worldsTask, themeAffinitiesTask);

            var definitions = definitionsTask.Result;
            var currentDefinition = definitions.FirstOrDefault(d =>
                string.Equals(d.Key, currentBinding.Key, StringComparison.OrdinalIgnoreCase));

            if (currentDefinition is not null && !string.IsNullOrWhiteSpace(currentDefinition.WorldKey))
            {
                var visitedKeys = run.Rooms
                    .Select(r => r.CatalogBinding?.Key)
                    .Where(key => key is not null)
                    .Select(key => key!)
                    .ToArray();

                var selected = _roomReachabilitySelector.SelectNextRoom(
                    currentDefinition, definitions, worldsTask.Result, themeAffinitiesTask.Result,
                    nextRoomDepth, visitedKeys, run.Seed);

                if (selected is not null)
                {
                    return (MapThemeToScaffold(selected.Theme), selected.Theme, selected);
                }
            }
        }

        return await ResolveLegacyThemeRoomAsync(run, nextRoomDepth, cancellationToken);
    }

    /// <summary>
    /// SAL-4: the room-type vocabulary + rotation come from the catalog. The resolver returns
    /// a theme key (e.g. "Fear"); we map it to a procedural scaffold enum only for the
    /// template/profile/boss machinery, and keep the key for room selection. Also the fallback
    /// a null-key legacy Exit resolves to at confirmation time (see
    /// GenerateSpecificRoomAsync) — content with no reachability graph never got a fixed
    /// destination when its exit was placed, so this is where its destination is finally rolled.
    /// </summary>
    private async Task<(RoomType RoomType, string ThemeKey, CatalogRoomDefinition? PreResolvedDefinition)> ResolveLegacyThemeRoomAsync(
        Run run, int nextRoomDepth, CancellationToken cancellationToken)
    {
        var nextRoomTypeKey = await _catalogRoomTypeResolver.ResolveNextRoomTypeKeyAsync(
            run.Seed,
            nextRoomDepth,
            run.CurrentRoom.RoomType.ToString(),
            cancellationToken);

        // No PreResolvedDefinition here on purpose — mirrors the original behavior exactly:
        // the actual catalog definition is picked later, by AttachCatalogRoomAsync's own
        // weighted roll (SelectRoomDefinition), not here.
        return (MapThemeToScaffold(nextRoomTypeKey), nextRoomTypeKey, null);
    }

    // SAL-2: bind the generated room to a catalog RoomDefinition (Pistburg, etc.)
    // whose theme matches the resolved room type, within the depth window, picked
    // deterministically by weight. No match → the room stays purely procedural.
    // Best-effort map of a catalog theme key to a procedural scaffold enum (templates,
    // generation profile, boss). Themes with no enum match (e.g. "Fear") fall back to a
    // neutral scaffold — the room's real identity comes from its catalog binding, not the enum.
    private static RoomType MapThemeToScaffold(string themeKey)
        => Enum.TryParse<RoomType>(themeKey, ignoreCase: true, out var roomType)
            ? roomType
            : RoomType.Memory;

    private Task<Room> GenerateRoomShapeAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken,
        PalaceRoomState palaceState,
        string? catalogRoomKey = null,
        string? bossDefinitionKey = null)
    {
        return _gridRoomGenerator.GenerateAsync(
            seed, generatorVersion, roomDepth, roomType, random, cancellationToken, palaceState,
            catalogRoomKey, bossDefinitionKey);
    }

    private async Task AttachCatalogRoomAsync(
        Room room,
        string themeKey,
        string seed,
        int roomDepth,
        CancellationToken cancellationToken)
    {
        var definitions = await _catalogContentGateway.ListRoomDefinitionsAsync(cancellationToken);
        var selected = SelectRoomDefinition(definitions, themeKey, roomDepth, seed);

        if (selected is null)
        {
            return;
        }

        AttachCatalogRoom(room, selected);
    }

    private static void AttachCatalogRoom(Room room, CatalogRoomDefinition selected)
    {
        room.AttachCatalogBinding(new CatalogRoomBinding(
            selected.Key,
            selected.DisplayName,
            selected.NarrativeText,
            selected.EnemyPoolKey,
            selected.RewardPoolKey,
            selected.LawPoolKey,
            selected.CursePoolKey,
            selected.IsUnique));
    }

    private static CatalogRoomDefinition? SelectRoomDefinition(
        IReadOnlyCollection<CatalogRoomDefinition> definitions,
        string themeKey,
        int roomDepth,
        string seed)
    {

        var eligible = definitions
            .Where(d => string.Equals(d.Theme, themeKey, StringComparison.OrdinalIgnoreCase))
            .Where(d => roomDepth >= d.MinDepth && roomDepth <= d.MaxDepth)
            .OrderBy(d => d.Key, StringComparer.Ordinal)
            .ToArray();

        if (eligible.Length == 0)
        {
            return null;
        }

        var totalWeight = eligible.Sum(d => Math.Max(1, d.BaseWeight));
        var roll = DeterministicCombatRoll.UnitInterval($"{seed}|room-def|{roomDepth}|{themeKey}");
        var target = roll * totalWeight;

        var cumulative = 0.0;
        foreach (var definition in eligible)
        {
            cumulative += Math.Max(1, definition.BaseWeight);
            if (target < cumulative)
            {
                return definition;
            }
        }

        return eligible[^1];
    }

    private static string? ResolveActiveClimate(Run run)
    {
        var modifier = run.RunModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == run.CurrentRoomId.Value)
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
