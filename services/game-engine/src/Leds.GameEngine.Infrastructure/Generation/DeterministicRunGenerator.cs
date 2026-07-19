using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Combats.Typing;
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
    private readonly IMapRoomGenerator _mapRoomGenerator;
    private readonly IGridRoomGenerator _gridRoomGenerator;
    private readonly IRunPsycheEvolver _psycheEvolver;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public DeterministicRunGenerator(
        ISeededRandomFactory randomFactory,
        ICatalogRoomTypeResolver catalogRoomTypeResolver,
        IRoomReachabilitySelector roomReachabilitySelector,
        IPalaceRoomStateResolver palaceRoomStateResolver,
        IMapRoomGenerator mapRoomGenerator,
        IGridRoomGenerator gridRoomGenerator,
        IRunPsycheEvolver psycheEvolver,
        ICatalogContentGateway catalogContentGateway)
    {
        _randomFactory = randomFactory;
        _catalogRoomTypeResolver = catalogRoomTypeResolver;
        _roomReachabilitySelector = roomReachabilitySelector;
        _palaceRoomStateResolver = palaceRoomStateResolver;
        _mapRoomGenerator = mapRoomGenerator;
        _gridRoomGenerator = gridRoomGenerator;
        _psycheEvolver = psycheEvolver;
        _catalogContentGateway = catalogContentGateway;
    }

    public string GeneratorVersion => DefaultRoomMapLayoutTemplates.GeneratorVersion;

    public string MarkovMatrixVersion => StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

    public string GenerateSeed()
    {
        return $"seed-{Guid.NewGuid():N}";
    }

    public async Task<Room> GenerateInitialRoomAsync(
        string seed,
        CancellationToken cancellationToken = default,
        RunExplorationMode explorationMode = RunExplorationMode.Classic)
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
                    PalaceRoomState.Neutral, explorationMode);
                AttachCatalogRoom(entryScaffold, entryRoom);
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
                    PalaceRoomState.Neutral,
                    explorationMode);

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

        return room;
    }

    public async Task<Room> GenerateNextRoomAsync(
        Run run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var nextRoomDepth = run.CurrentDepth + 1;
        var matrixVersion = string.IsNullOrWhiteSpace(run.MarkovMatrixVersion)
            ? MarkovMatrixVersion
            : run.MarkovMatrixVersion;

        // Inconscient du Palais : distribution latente dérivée de l'historique de salles
        // (déterministe). Persiste (Advance), accumule (nudge) et biaise la génération.
        var psyche = _psycheEvolver.Evolve(run);

        var (roomType, themeKey, preResolvedDefinition) = await ResolveNextRoomAsync(run, nextRoomDepth, cancellationToken);

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

        var room = await GenerateRoomShapeAsync(
            run.Seed,
            GeneratorVersion,
            nextRoomDepth,
            roomType,
            random,
            cancellationToken,
            palaceState,
            run.ExplorationMode);

        if (preResolvedDefinition is not null)
        {
            AttachCatalogRoom(room, preResolvedDefinition);
        }
        else
        {
            await AttachCatalogRoomAsync(room, themeKey, run.Seed, nextRoomDepth, cancellationToken);
        }

        return room;
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

        // SAL-4: the room-type vocabulary + rotation come from the catalog. The resolver
        // returns a theme key (e.g. "Fear"); we map it to a procedural scaffold enum only
        // for the template/profile/boss machinery, and keep the key for room selection.
        //
        // Mina's legendary "Protection de Him'Lit" tightens the boss-recurrence interval
        // (10 rooms -> ~7) for a ~+50% encounter frequency, owned — not equipped.
        var bossInterval = run.HimLitProtectionEnabled
            ? (int)Math.Round(CatalogMarkovRoomTypeResolver.BossInterval / 1.5)
            : CatalogMarkovRoomTypeResolver.BossInterval;

        var nextRoomTypeKey = await _catalogRoomTypeResolver.ResolveNextRoomTypeKeyAsync(
            run.Seed,
            nextRoomDepth,
            run.CurrentRoom.RoomType.ToString(),
            cancellationToken,
            bossInterval);

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

    /// <summary>
    /// Chooses which room shape to generate — the Classic row/lane DAG
    /// (<see cref="IMapRoomGenerator"/>) or the Tactical free-roam grid
    /// (<see cref="IGridRoomGenerator"/>) — based on the run's exploration mode. Catalog
    /// binding, theme resolution, and every other generation concern is identical either way.
    /// </summary>
    private Task<Room> GenerateRoomShapeAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken,
        PalaceRoomState palaceState,
        RunExplorationMode explorationMode)
    {
        return explorationMode == RunExplorationMode.Tactical
            ? _gridRoomGenerator.GenerateAsync(seed, generatorVersion, roomDepth, roomType, random, cancellationToken, palaceState)
            : _mapRoomGenerator.GenerateAsync(seed, generatorVersion, roomDepth, roomType, random, cancellationToken, palaceState);
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
