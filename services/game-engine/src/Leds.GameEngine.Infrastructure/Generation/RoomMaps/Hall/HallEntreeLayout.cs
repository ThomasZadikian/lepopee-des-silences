namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;

/// <summary>
/// The Hall d'entrée's authored, fixed geometry — SFD Hall d'entrée §III/§VIII: "squelette
/// authored fixe" (tapis axis, four pillars, seven-step staircase, salons/alcoves), ported
/// cell-for-cell from the reference implementation (<c>Hall d'entrée - salle vivante.dc.html</c>,
/// <c>salle-render.js</c>'s <c>carveRoom</c>) rather than re-derived from the prose SFD alone,
/// since the SFD deliberately leaves exact footprint/width as authored variation
/// ("variations limitées de largeur, forme et emprise").
/// <para>
/// Deliberately excludes decor, event pools, and the boss's real identity — see
/// <see cref="Build"/>'s remarks. This is geometry only, the first concrete consumer of the
/// generic <c>RoomGrid</c>/<c>RoomNpc</c> engine built in Ensembles 1-3, not a claim that the
/// Hall's content is finished.
/// </para>
/// </summary>
public static class HallEntreeLayout
{
    public const int Width = 26;
    public const int Height = 18;

    public const int StartX = 12;
    public const int StartY = 15;

    /// <summary>The four marble pillars — SFD §III/§VIII: "exactement quatre piliers de marbre,
    /// toujours situés dans le cœur de la salle". Landmarks, not collision: the reference
    /// implementation renders them as pure decor on walkable floor (no obstacle layer at all),
    /// so <see cref="Build"/> does NOT add these to the returned <c>Obstacles</c> — they're
    /// exposed here as position data for later decor/event authoring instead.</summary>
    public static readonly IReadOnlyList<(int X, int Y)> Pillars =
    [
        (9, 6), (15, 6), (9, 11), (15, 11),
    ];

    /// <summary>
    /// Five minor, non-narrative curiosities (SFD §VII: "l'exploration transmet autant de lore
    /// que les dialogues") placed at authored positions echoing the reference's small decor
    /// objects (registre, papiers, malles) — mechanically ordinary <c>NodeEventType.Item</c>
    /// nodes, no narrative text authored yet. They do not stand in for the Hall's real signature
    /// content (SFD §VI), which must come from an authored Catalog definition.
    /// </summary>
    public static readonly IReadOnlyList<(int X, int Y)> CurioCells =
    [
        (6, 4), (7, 8), (20, 4), (23, 8), (6, 15),
    ];

    /// <summary>The east threshold cell toward the Pièce des émotions (SFD §III: "Est: Accès
    /// vers la Pièce des émotions") — the same cell <see cref="Build"/> already punches as a door.
    /// Named here, rather than re-deriving the literal, so the protocole chantier's "ne pas
    /// s'approcher de la Pièce des émotions" rule (SFD §V) has one source of truth for where
    /// "approaching" starts.</summary>
    public const int EmotionsThresholdX = 24;
    public const int EmotionsThresholdY = 10;

    /// <summary>The tapis (carpet) axis — a rendering/rule zone, not a structural feature: it
    /// doesn't affect floor/obstacles/elevation. Recorded here so Ensemble 4's later protocol
    /// chantier (the "essuyez vos pieds" LocalRule, SFD §V) has an authored cell set to point at
    /// without re-deriving it. x∈[10,14], y∈[4,16] — SFD §III's tapis axis, from entry to the
    /// staircase.</summary>
    public static IReadOnlyCollection<(int X, int Y)> TapisCells { get; } = BuildTapisCells();

    private static List<(int X, int Y)> BuildTapisCells()
    {
        var cells = new List<(int X, int Y)>();
        for (var y = 4; y <= 16; y++)
        {
            for (var x = 10; x <= 14; x++)
            {
                cells.Add((x, y));
            }
        }

        return cells;
    }

    /// <summary>Surface material key for the tapis band — the one value <see cref="TapisCells"/>
    /// is painted with in <see cref="RoomGrid.SurfaceOverrides"/>.</summary>
    public const string TapisSurfaceKey = "carpet";

    /// <summary>Decor key for a pillar placement — one of the mutualized decor kinds (not
    /// Hall-unique), matching <see cref="Pillars"/>'s own cells.</summary>
    public const string PillarDecorKey = "column";

    /// <summary>
    /// Authored decor beyond the four pillars — the reference handoff's "Secteurs" table (README
    /// §2), positioned to avoid every node/PNJ/pillar/door cell already placed elsewhere in this
    /// class. Mixes the Hall's six unique props (<c>hall*</c> keys, never recycled outside this
    /// room — SFD Hall d'entrée's "un décor unique ne se recycle jamais") with shared salon props
    /// (<c>armchair</c>, <c>salonTable</c>, <c>silverware</c>, <c>glassware</c>, <c>teaService</c>)
    /// that exist in multiple rooms. Every position here was cross-checked against
    /// <see cref="CurioCells"/> and <see cref="HallEntreeCasting.Roster"/> at authoring time —
    /// there is no runtime collision check, same as <see cref="Pillars"/>, since decor placement
    /// is purely cosmetic and never validated against occupancy the way nodes/NPCs are.
    /// </summary>
    public static readonly IReadOnlyList<(int X, int Y, string Key)> SectorDecor =
    [
        // Salon ouest — salon d'attente (interior x∈[1,7], y∈[4,8]; door at (8,6)).
        (2, 4, "armchair"), (2, 7, "armchair"), (6, 5, "armchair"),
        (3, 5, "salonTable"), (3, 6, "teaService"), (4, 8, "hallLustre"),

        // Salon est — table dressée (interior x∈[18,24], y∈[4,8]; door at (17,6)).
        (19, 5, "silverware"), (19, 7, "glassware"), (21, 5, "salonTable"),
        (18, 4, "armchair"), (24, 7, "armchair"),

        // Nef / axe du tapis — two suspended hallLustre plus the stopped clock.
        (12, 7, "hallLustre"), (12, 12, "hallLustre"), (15, 9, "hallHorloge"),

        // Vestibule (y∈[15,16], x∈[9,15]) — accueil.
        (10, 16, "hallRegistre"), (14, 16, "hallPortemanteau"), (9, 16, "hallMalles"),

        // Alcôves sud-ouest/sud-est — rolled carpet stashed in each.
        (2, 14, "hallTapisRoule"), (21, 14, "hallTapisRoule"),
    ];

    public sealed record Result(
        bool[] Floor,
        int[] Elevation,
        HashSet<(int X, int Y)> Obstacles,
        IReadOnlyList<(int X, int Y)> Doors,
        IReadOnlyDictionary<(int X, int Y), string> SurfaceOverrides,
        IReadOnlyDictionary<(int X, int Y), string> DecorPlacements);

    /// <summary>
    /// Builds the Hall's fixed floor/elevation/obstacles/doors. Every coordinate below is
    /// authored, not rolled — this is the "squelette authored fixe" the SFD requires, distinct
    /// from the procedural rooms <c>GridRoomGenerator</c> otherwise produces.
    /// </summary>
    public static Result Build()
    {
        // Interior floor, bordered by the outer wall — same "blank room" convention the
        // reference implementation and GridRoomGenerator's own random carving both start from.
        var floor = new bool[Width * Height];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                floor[(y * Width) + x] = x > 0 && x < Width - 1 && y > 0 && y < Height - 1;
            }
        }

        var doors = new List<(int X, int Y)>();

        // Two salons flanking the nave, doors turned toward the tapis (SFD §III: "salons et
        // alcôves latéraux"), two service alcoves further south sharing their outer walls.
        CarveSubRoom(floor, 1, 3, 8, 9, [(8, 6)], doors);
        CarveSubRoom(floor, 17, 3, 24, 9, [(17, 6)], doors);
        CarveSubRoom(floor, 1, 11, 7, 15, [(7, 13)], doors);
        CarveSubRoom(floor, 18, 11, 24, 15, [(18, 13)], doors);

        // The two lateral corridor thresholds (Tortue west, Émotions east) and the south
        // entrance — region boundaries for the client's enceinte lighting, same convention as
        // every other sub-room door (RoomGrid.Doors's own remarks).
        doors.Add((1, 10));
        doors.Add((EmotionsThresholdX, EmotionsThresholdY));
        doors.Add((12, 16));

        var elevation = new int[Width * Height];
        // The staircase: three flights (3+2+2 = seven marches, SFD §III/§VIII) climbing north.
        for (var x = 9; x <= 15; x++)
        {
            elevation[(3 * Width) + x] = 1;
            elevation[(2 * Width) + x] = 2;
            elevation[(1 * Width) + x] = 3;
        }

        // Pillars are landmarks, not collision: the reference implementation places them as pure
        // decor on otherwise walkable floor (no obstacle/pathfinding layer at all), and one of
        // them — (9,6) — sits directly beside the west salon's only door, which would be
        // permanently unreachable if it blocked movement. RoomGrid.Obstacles stays empty; see
        // Pillars's own remarks for where the position data lives instead.
        var obstacles = new HashSet<(int X, int Y)>();

        var surfaceOverrides = TapisCells.ToDictionary(cell => cell, _ => TapisSurfaceKey);

        var decorPlacements = Pillars.ToDictionary(cell => cell, _ => PillarDecorKey);
        foreach (var (x, y, key) in SectorDecor)
        {
            decorPlacements[(x, y)] = key;
        }

        return new Result(floor, elevation, obstacles, doors, surfaceOverrides, decorPlacements);
    }

    /// <summary>
    /// Ports <c>salle-render.js</c>'s <c>carveRoom</c> verbatim: a sub-room's side is walled off
    /// UNLESS that side already sits on (or past) the room's own outer boundary, in which case the
    /// outer wall already serves and drawing a second one one cell in would leave a dead one-cell
    /// corridor between two parallel walls. When a side hugs the boundary this way, that
    /// coordinate is snapped to the true edge (<see cref="Width"/>/<see cref="Height"/> - 2) even
    /// if the caller passed a slightly interior value — same snapping the reference performs.
    /// </summary>
    private static void CarveSubRoom(
        bool[] floor, int x0, int y0, int x1, int y1,
        IReadOnlyList<(int X, int Y)> doorCells, List<(int X, int Y)> doors)
    {
        var hugW = x0 <= 2;
        var hugN = y0 <= 2;
        var hugE = x1 >= Width - 3;
        var hugS = y1 >= Height - 3;

        if (hugW) x0 = 1;
        if (hugN) y0 = 1;
        if (hugE) x1 = Width - 2;
        if (hugS) y1 = Height - 2;

        if (!hugN) for (var x = x0; x <= x1; x++) floor[(y0 * Width) + x] = false;
        if (!hugS) for (var x = x0; x <= x1; x++) floor[(y1 * Width) + x] = false;
        if (!hugW) for (var y = y0; y <= y1; y++) floor[(y * Width) + x0] = false;
        if (!hugE) for (var y = y0; y <= y1; y++) floor[(y * Width) + x1] = false;

        foreach (var (doorX, doorY) in doorCells)
        {
            if (doorX < 1 || doorY < 1 || doorX > Width - 2 || doorY > Height - 2)
            {
                continue;
            }

            floor[(doorY * Width) + doorX] = true;
            doors.Add((doorX, doorY));
        }
    }
}
