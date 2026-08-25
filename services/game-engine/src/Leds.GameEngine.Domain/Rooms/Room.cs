using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Protocol;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<MapNode> _nodes;
    private readonly List<RoomNpc> _roomNpcs;
    private readonly List<LocalRuleState> _localRuleStates;

    /// <summary>
    /// The node currently in the Select/Resolve interaction slot (set by
    /// <see cref="EnterNodeAtPartyPosition"/>). Needed
    /// because a grid node stays <see cref="NodeState.Resolved"/> forever once resolved — so
    /// after a second node is resolved, scanning _nodes by state alone would match more than
    /// one node. See CurrentSelectedNode/CurrentResolvedNode below.
    /// </summary>
    private NodeId? _currentGridNodeId;

    private Room(
        RoomId id,
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile? bossProfile,
        RoomState state,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid,
        IEnumerable<RoomNpc>? roomNpcs = null,
        IEnumerable<LocalRuleState>? localRuleStates = null)
    {
        Id = id;
        Depth = depth;
        RoomType = roomType;
        PalaceState = palaceState;
        Theme = theme;
        BossProfile = bossProfile;
        State = state;
        CurrentNodeDepth = 0;
        _nodes = nodes.ToList();
        MaxNodeDepth = _nodes.Count > 0 ? _nodes.Max(n => n.Row) : 0;
        LayoutTemplateKey = layoutTemplateKey;
        LayoutTemplateVersion = layoutTemplateVersion;
        Grid = grid;
        _roomNpcs = roomNpcs?.ToList() ?? new List<RoomNpc>();
        _localRuleStates = localRuleStates?.ToList() ?? new List<LocalRuleState>();
    }


    public RoomId Id { get; }

    public int Depth { get; }

    public RoomType RoomType { get; }

    public PalaceRoomState PalaceState { get; private set; }

    public string Theme { get; }

    /// <summary>
    /// Optional authored encounter for this room. A room is an exploration space first; a boss
    /// is present only when its content definition explicitly declares one.
    /// </summary>
    public RoomBossProfile? BossProfile { get; }

    /// <summary>
    /// Inert vestige of the Classic row/lane DAG's depth cursor — no longer advanced by
    /// anything (grid rooms never had a meaningful "current row"), but left in place rather
    /// than ripped out of persistence/DTOs for a field with no functional cost to keep.
    /// </summary>
    public int CurrentNodeDepth { get; private set; }

    public int MaxNodeDepth { get; }

    public int TotalNodeCount => _nodes.Count;

    /// <summary>
    /// Nodes excluding Exit — the count the "6 to 30" room-size invariant
    /// (<see cref="Runs.Run.StartNew"/>) actually means: doorways are structural, appended
    /// after content generation via <see cref="AttachExitNodes"/>, and shouldn't be able to
    /// push an already-full room over the ceiling.
    /// </summary>
    public int ContentNodeCount => _nodes.Count(n => n.EventType != NodeEventType.Exit);

    public RoomState State { get; private set; }

    public IReadOnlyCollection<MapNode> Nodes => _nodes.AsReadOnly();

    /// <summary>
    /// Physically present, positioned NPCs — see <see cref="RoomNpc"/>'s own remarks for why
    /// this is a separate collection from <see cref="Nodes"/>. Empty for every room until a
    /// generator actually populates one (see <see cref="AddRoomNpc"/>) — no room does yet.
    /// </summary>
    public IReadOnlyCollection<RoomNpc> RoomNpcs => _roomNpcs.AsReadOnly();

    /// <summary>
    /// This room instance's run-scoped progress against whichever <see cref="LocalRule"/>s a
    /// generator attached (see <see cref="AddLocalRuleState"/>) — empty for every room until a
    /// generator actually populates one (no room does yet). Deliberately holds only the state,
    /// never the authored <see cref="LocalRule"/> itself: the rule's condition/consequences are
    /// Catalog content, resolved by the Application layer when it evaluates a trigger, exactly
    /// like <see cref="Npcs.NpcRelationship"/> vs its Catalog dialogue graph.
    /// </summary>
    public IReadOnlyCollection<LocalRuleState> LocalRuleStates => _localRuleStates.AsReadOnly();

    public string? LayoutTemplateKey { get; }

    public string? LayoutTemplateVersion { get; }

    /// <summary>Free-movement grid overlay.</summary>
    public RoomGrid Grid { get; }

    /// <summary>Persistence-facing view of <see cref="_currentGridNodeId"/> — see its own doc comment.</summary>
    public NodeId? CurrentGridNodeId => _currentGridNodeId;

    public CatalogRoomBinding? CatalogBinding { get; private set; }

    public void AttachCatalogBinding(CatalogRoomBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        CatalogBinding = binding;
    }

    public IReadOnlyCollection<MapNode> AvailableNodes =>
        VisibleNodes.Where(n => n.State == NodeState.Available).ToArray();

    /// <summary>
    /// Nodes the party knows about: revealed by fog of war AND not still hiding. Hidden nodes
    /// are withheld here rather than only in the DTO, so nothing downstream — the projection
    /// sent to the client included — can accidentally advertise a node the player is supposed
    /// to have to search for.
    /// </summary>
    public IReadOnlyCollection<MapNode> VisibleNodes =>
        _nodes.Where(n => Grid.RevealedNodeIds.Contains(n.Id) && !n.IsHidden).ToArray();

    /// <summary>
    /// Cells holding a cache nobody has found yet — position only, deliberately without the
    /// node behind it. The room shows a slab that rings hollow; what is underneath stays
    /// unknown until it is searched, which is the whole point of spending budget on a search.
    /// Revealed once found, so it drops out of this list and the node itself becomes visible.
    /// </summary>
    public IReadOnlyCollection<(int X, int Y)> HintCells => _nodes
        .Where(node => node.IsHidden)
        .Select(node => (node.Lane, node.Row))
        .ToArray();

    /// <summary>
    /// Hidden nodes close enough to the party to be searched out. Kept separate from
    /// <see cref="VisibleNodes"/>: the client is told a search would find something nearby, never
    /// what or exactly where.
    /// </summary>
    public IReadOnlyCollection<MapNode> SearchableNodes => _nodes
        .Where(n => n.IsHidden && IsWithinSearchRange(n))
        .ToArray();

    /// <summary>The single node currently in <see cref="NodeState.Selected"/>, if any.</summary>
    public MapNode? CurrentSelectedNode => CurrentGridInteractionNode(NodeState.Selected);

    /// <summary>Counterpart of <see cref="CurrentSelectedNode"/> for <see cref="NodeState.Resolved"/>.</summary>
    public MapNode? CurrentResolvedNode => CurrentGridInteractionNode(NodeState.Resolved);

    /// <summary>
    /// Lookup by the tracked <see cref="_currentGridNodeId"/> instead of scanning all nodes by
    /// state — a resolved node never reverts, so more than one node can be Resolved at once;
    /// only the one we most recently selected/resolved is "current".
    /// </summary>
    private MapNode? CurrentGridInteractionNode(NodeState expectedState)
    {
        if (_currentGridNodeId is not { } nodeId)
        {
            return null;
        }

        var node = _nodes.SingleOrDefault(n => n.Id == nodeId);
        return node?.State == expectedState ? node : null;
    }

    public static Room Create(
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile? bossProfile,
        IEnumerable<MapNode> nodes,
        int gridWidth,
        int gridHeight,
        int movementBudget,
        int startX,
        int startY,
        string layoutTemplateKey,
        string layoutTemplateVersion,
        IReadOnlyList<int>? elevation = null,
        IReadOnlyCollection<(int X, int Y)>? obstacles = null,
        IReadOnlyList<bool>? floorCells = null,
        IReadOnlyCollection<(int X, int Y)>? doorCells = null,
        IReadOnlyDictionary<(int X, int Y), string>? surfaceOverrides = null,
        IReadOnlyDictionary<(int X, int Y), string>? decorPlacements = null)
    {
        if (depth is < 0 or > 10)
        {
            throw new DomainException("Room depth must be between 0 and 10.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new DomainException("Room theme is required.");
        }

        if (string.IsNullOrWhiteSpace(layoutTemplateKey))
        {
            throw new DomainException("Layout template key is required.");
        }

        if (string.IsNullOrWhiteSpace(layoutTemplateVersion))
        {
            throw new DomainException("Layout template version is required.");
        }

        var nodeList = nodes?.ToList()
            ?? throw new DomainException("Room nodes are required.");

        if (nodeList.Count < 1)
        {
            throw new DomainException("A grid room must contain at least 1 node.");
        }

        var bossNodes = nodeList.Where(n => n.IsBoss).ToArray();

        if (bossNodes.Length > 1)
        {
            throw new DomainException("A room cannot contain more than one boss node.");
        }

        if ((bossNodes.Length == 1) != (bossProfile is not null))
        {
            throw new DomainException("A boss profile and a boss node must be declared together.");
        }

        if (nodeList.Any(n => n.State != NodeState.Available))
        {
            throw new DomainException("Every grid node must start as available.");
        }

        foreach (var node in nodeList)
        {
            if (node.Lane < 0 || node.Lane >= gridWidth || node.Row < 0 || node.Row >= gridHeight)
            {
                throw new DomainException("Every grid node must be within the grid bounds.");
            }
        }

        if (nodeList.Select(n => (n.Lane, n.Row)).Distinct().Count() != nodeList.Count)
        {
            throw new DomainException("Two grid nodes cannot occupy the same cell.");
        }

        if (startX < 0 || startX >= gridWidth || startY < 0 || startY >= gridHeight)
        {
            throw new DomainException("Grid start position must be within the grid bounds.");
        }

        if (nodeList.Any(n => n.Lane == startX && n.Row == startY))
        {
            throw new DomainException("No node can occupy the party's starting cell.");
        }

        var grid = RoomGrid.CreateInitial(
            gridWidth, gridHeight, movementBudget, startX, startY, nodeList,
            elevation, obstacles, floorCells, doorCells, surfaceOverrides, decorPlacements);

        // A node standing on a hole in the room's shape would be unreachable and unpaintable.
        // Checked after the grid exists because the floor mask is validated/defaulted there.
        foreach (var node in nodeList)
        {
            if (!grid.IsFloor(node.Lane, node.Row))
            {
                throw new DomainException("Every grid node must stand on one of the room's floor cells.");
            }
        }

        // Defense-in-depth: generation is expected to guarantee every node is reachable from the
        // start (obstacle placement is connectivity-checked at generation time), but a room built
        // by hand (tests, future authoring tools) gets the same guarantee enforced here.
        foreach (var node in nodeList)
        {
            if (grid.FindPath(node.Lane, node.Row) is null)
            {
                throw new DomainException(
                    "Every grid node must be reachable from the party's starting position.");
            }
        }

        return new Room(
            RoomId.New(),
            depth,
            roomType,
            palaceState,
            theme.Trim(),
            bossProfile,
            RoomState.Active,
            nodeList,
            layoutTemplateKey.Trim(),
            layoutTemplateVersion.Trim(),
            grid);
    }

    public static Room Create(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile? bossProfile,
        IEnumerable<MapNode> nodes,
        int gridWidth,
        int gridHeight,
        int movementBudget,
        int startX,
        int startY,
        string layoutTemplateKey,
        string layoutTemplateVersion,
        IReadOnlyList<int>? elevation = null,
        IReadOnlyCollection<(int X, int Y)>? obstacles = null)
    {
        return Create(
            depth,
            roomType,
            PalaceRoomState.Neutral,
            theme,
            bossProfile,
            nodes,
            gridWidth,
            gridHeight,
            movementBudget,
            startX,
            startY,
            layoutTemplateKey,
            layoutTemplateVersion,
            elevation,
            obstacles);
    }

    public MapNode GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    /// <summary>
    /// Appends the room's exits — one per reachable catalog room, fixed once at generation
    /// time (see DeterministicRunGenerator's post-processing step) so every real branch is
    /// visible to the player instead of a silent weighted pick. Added after the room already
    /// exists (the grid and its other nodes are needed to place them), so it mirrors
    /// <see cref="Create"/>'s own node-placement invariants rather than <see cref="Create"/>
    /// enforcing them itself.
    /// </summary>
    public void AttachExitNodes(IEnumerable<MapNode> exitNodes)
    {
        var nodeList = exitNodes?.ToList() ?? throw new DomainException("Exit nodes are required.");

        if (nodeList.Count == 0)
        {
            return;
        }

        if (nodeList.Any(node => node.EventType != NodeEventType.Exit))
        {
            throw new DomainException("AttachExitNodes only accepts Exit nodes.");
        }

        if (nodeList.Select(node => (node.Lane, node.Row)).Distinct().Count() != nodeList.Count)
        {
            throw new DomainException("Two exit nodes cannot occupy the same cell.");
        }

        foreach (var node in nodeList)
        {
            if (node.Lane < 0 || node.Lane >= Grid.Width || node.Row < 0 || node.Row >= Grid.Height)
            {
                throw new DomainException("Every exit node must be within the grid bounds.");
            }

            if (node.Lane == Grid.StartX && node.Row == Grid.StartY)
            {
                throw new DomainException("No exit node can occupy the party's starting cell.");
            }

            if (!Grid.IsFloor(node.Lane, node.Row))
            {
                throw new DomainException("Every exit node must stand on one of the room's floor cells.");
            }

            if (_nodes.Any(existing => existing.Lane == node.Lane && existing.Row == node.Row))
            {
                throw new DomainException("An exit node cannot occupy a cell already taken by another node.");
            }

            if (Grid.FindPath(node.Lane, node.Row) is null)
            {
                throw new DomainException("Every exit node must be reachable from the party's starting position.");
            }
        }

        _nodes.AddRange(nodeList);
    }

    /// <summary>
    /// Places a physically present NPC in the room — mirrors <see cref="AttachExitNodes"/>'s
    /// own shape (added once the grid already exists, so its placement can be validated against
    /// it) but for <see cref="RoomNpc"/> rather than <see cref="MapNode"/>.
    /// </summary>
    public void AddRoomNpc(RoomNpc npc)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (npc.X < 0 || npc.X >= Grid.Width || npc.Y < 0 || npc.Y >= Grid.Height)
        {
            throw new DomainException("Every room NPC must be within the grid bounds.");
        }

        if (!Grid.IsFloor(npc.X, npc.Y))
        {
            throw new DomainException("Every room NPC must stand on one of the room's floor cells.");
        }

        _roomNpcs.Add(npc);
    }

    /// <summary>Registers a spatially valid direct interaction and returns the actor to the Run.</summary>
    public RoomNpc InteractWithRoomNpc(RoomNpcId id)
    {
        var npc = _roomNpcs.FirstOrDefault(n => n.Id == id)
            ?? throw new DomainException("Room NPC does not belong to this room.");

        var distance = Math.Abs(npc.X - Grid.PartyX) + Math.Abs(npc.Y - Grid.PartyY);
        if (distance > 1)
            throw new DomainException("The party must stand next to a room NPC to interact.");

        npc.NoticeParty();
        return npc;
    }

    /// <summary>
    /// Registers a <see cref="LocalRule"/>'s run-scoped tracking for this room instance — mirrors
    /// <see cref="AddRoomNpc"/>'s shape but keyed rather than position-validated, since a rule's
    /// condition cells/target are validated when the rule itself is authored, not here.
    /// </summary>
    public void AddLocalRuleState(LocalRuleState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_localRuleStates.Any(s => s.LocalRuleKey == state.LocalRuleKey))
        {
            throw new DomainException($"Local rule '{state.LocalRuleKey}' is already tracked in this room.");
        }

        _localRuleStates.Add(state);
    }

    /// <summary>Lookup by key for the Application layer to fetch-then-evaluate against the
    /// matching Catalog <see cref="LocalRule"/> — null if this room never tracked that rule.</summary>
    public LocalRuleState? GetLocalRuleState(string localRuleKey) =>
        _localRuleStates.FirstOrDefault(s => s.LocalRuleKey == localRuleKey);

    /// <summary>
    /// Room-wide difficulty selector: sets every still-available combat-flavored node's
    /// tier to the chosen value at once — a node already resolved, or already committed to
    /// (Selected), keeps whatever tier it had. Silently a no-op if the room has no such node
    /// left (e.g. every combat already resolved) rather than throwing, since "set the room's
    /// difficulty" is a broad player intent, not a per-node action.
    /// </summary>
    public void SetDesiredRiskTierForPendingCombatNodes(RiskTier tier)
    {
        foreach (var node in _nodes.Where(n =>
                     MapNode.IsCombatFlavored(n.EventType) && n.State == NodeState.Available))
        {
            node.SetCombatRiskTier(tier);
        }
    }

    /// <summary>
    /// Cells the party cannot walk THROUGH right now: unresolved blocking nodes. A lock stops
    /// blocking once it has been dealt with, otherwise resolving it would leave the room
    /// permanently severed. Hidden nodes are excluded — an undiscovered node cannot bar a
    /// corridor the party has no way of knowing about.
    /// </summary>
    private IReadOnlySet<(int X, int Y)> CurrentTransitBlockers => _nodes
        .Where(node => node.BlocksTransit && !node.IsHidden && node.State != NodeState.Resolved)
        .Select(node => (node.Lane, node.Row))
        .ToHashSet();

    /// <summary>
    /// Moves the party across the grid along the cheapest walkable route (obstacles and holes
    /// routed around, elevation climbs priced in, unresolved blocking nodes never crossed — see
    /// <see cref="RoomGrid.FindPath"/>), revealing fog of war along the way. Traversal cost is
    /// reported for pacing/telemetry but is not a consumable exploration resource.
    /// <para>
    /// If the walk steps onto a contact-triggered node it stops there, is charged only for the
    /// ground actually covered, and the node is selected immediately — the same interaction
    /// <see cref="EnterNodeAtPartyPosition"/> performs, minus the choice.
    /// </para>
    /// </summary>
    public sealed record PartyMoveResult(
        IReadOnlyList<(int X, int Y)> TraversedCells,
        int SpentMovement,
        NodeId? TriggeredNodeId);

    public PartyMoveResult MoveParty(int targetX, int targetY)
    {
        if (State is not RoomState.Active)
        {
            throw new DomainException("Room is not waiting for party movement.");
        }

        if (targetX < 0 || targetX >= Grid.Width || targetY < 0 || targetY >= Grid.Height)
        {
            throw new DomainException("Target position is outside the grid bounds.");
        }

        if (targetX == Grid.PartyX && targetY == Grid.PartyY)
        {
            throw new DomainException("The party is already at the target position.");
        }

        var route = Grid.FindPath(targetX, targetY, CurrentTransitBlockers)
            ?? throw new DomainException("No walkable path to the target position.");

        var (startX, startY) = (Grid.PartyX, Grid.PartyY);
        var triggered = Grid.MoveTo(route.Path, route.Cost, _nodes);
        var traversed = route.Path
            .TakeWhile(cell =>
                cell != (Grid.PartyX, Grid.PartyY))
            .Append((Grid.PartyX, Grid.PartyY))
            .ToArray();

        if (triggered is not null)
        {
            triggered.Select();
            _currentGridNodeId = triggered.Id;
            State = RoomState.NodeSelected;
        }

        // One RoomNpc step per cell ACTUALLY entered (Contrat canonique §13: "chaque case
        // réellement parcourue constitue un pas déterministe"), not one step per MoveParty call —
        // a route spanning several cells gives a Hunter that many chances to react, not one.
        // `traversed` already excludes the party's pre-move cell (route.Path never includes the
        // start), so every element here is a newly entered cell — nothing to skip.
        foreach (var (stepX, stepY) in traversed)
        {
            foreach (var npc in _roomNpcs)
            {
                npc.Step(Grid, stepX, stepY);
                npc.RefreshAwareness(Grid, stepX, stepY);
            }
        }

        var traversalCost = 0;
        var (previousX, previousY) = (startX, startY);
        foreach (var (x, y) in traversed)
        {
            traversalCost += 1 + Math.Max(0, Grid.ElevationAt(x, y) - Grid.ElevationAt(previousX, previousY));
            (previousX, previousY) = (x, y);
        }

        return new PartyMoveResult(traversed, traversalCost, triggered?.Id);
    }

    // BALANCE KNOB — how far a search reaches, in cells (Chebyshev: the 8 cells around the
    // party plus its own). Deliberately tight: finding something has to be about standing in
    // the right place, not about sweeping the room from a distance.
    public const int SearchRadius = 1;

    private bool IsWithinSearchRange(MapNode node) =>
        Math.Abs(node.Lane - Grid.PartyX) <= SearchRadius
        && Math.Abs(node.Row - Grid.PartyY) <= SearchRadius;

    /// <summary>
    /// True when searching from where the party stands would actually turn something up.
    /// </summary>
    public bool CanSearchAtPartyPosition =>
        State is RoomState.Active && SearchableNodes.Count > 0;

    /// <summary>
    /// Searches the ground around the party, revealing every hidden node within
    /// <see cref="SearchRadius"/>. Search is an interaction, not an arbitrary global-resource
    /// sink; the room simply refuses it when there is nothing to find.
    /// </summary>
    public void SearchAtPartyPosition()
    {
        if (State is not RoomState.Active)
        {
            throw new DomainException("Room is not waiting for party movement.");
        }

        var found = SearchableNodes;

        if (found.Count == 0)
        {
            throw new DomainException("There is nothing to find here.");
        }

        foreach (var node in found)
        {
            node.Reveal();
        }

        Grid.RefreshVisibility(_nodes);
    }

    /// <summary>Selects the node currently occupied by the party.</summary>
    public void EnterNodeAtPartyPosition(NodeId nodeId)
    {
        if (State is not RoomState.Active)
        {
            throw new DomainException("Room is not waiting for a node selection.");
        }

        var node = GetNode(nodeId);

        if (node.Lane != Grid.PartyX || node.Row != Grid.PartyY)
        {
            throw new DomainException("The party is not standing on this node's cell.");
        }

        if (node.IsHidden)
        {
            throw new DomainException("This node has not been found yet.");
        }

        node.Select();
        _currentGridNodeId = nodeId;
        State = RoomState.NodeSelected;
    }

    public void ResolveSelectedNodeEvent()
    {
        if (State != RoomState.NodeSelected)
        {
            throw new DomainException("Room must have a selected node before resolving an event.");
        }

        var selectedNode = CurrentSelectedNode
            ?? throw new DomainException("No node has been selected in this room.");

        selectedNode.Resolve();

        // The boss no longer locks the room shut once defeated (see Run.ConfirmRoomExit) —
        // resolving it is just another resolved node, same as any other. RoomState.Completed
        // is never produced anymore (kept [Obsolete] on the enum for Rehydrate compatibility
        // with any row still persisted at that value).
        State = RoomState.NodeResolved;
    }

    /// <summary>There is no next layer to unlock in free exploration — this simply returns the
    /// room to free movement.</summary>
    public void ReturnToExploration()
    {
        if (State != RoomState.NodeResolved)
        {
            throw new DomainException("Current node event must be resolved before progressing.");
        }

        State = RoomState.Active;
    }

    public void ResetProgress()
    {
        if (State is RoomState.Active or RoomState.NodeSelected
            or RoomState.NodeResolved or RoomState.BossReached)
        {
            foreach (var node in _nodes)
            {
                node.ResetToGridAvailable();
            }

            Grid.ResetToInitial(_nodes);
            State = RoomState.Active;
            return;
        }

        throw new DomainException("Room is closed and cannot be reset.");
    }

    public static Room Rehydrate(
        RoomId id,
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile? bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid,
        NodeId? currentGridNodeId,
        IEnumerable<RoomNpc>? roomNpcs = null,
        IEnumerable<LocalRuleState>? localRuleStates = null)
    {
        var room = new Room(id, depth, roomType, palaceState, theme, bossProfile, state, nodes, layoutTemplateKey, layoutTemplateVersion, grid, roomNpcs, localRuleStates);
        room.CurrentNodeDepth = currentNodeDepth;
        room._currentGridNodeId = currentGridNodeId;
        return room;
    }

    public void DebugSetPalaceState(PalaceRoomState palaceState)
    {
        PalaceState = palaceState;
    }

    public static Room Rehydrate(
        RoomId id,
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile? bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid,
        NodeId? currentGridNodeId,
        IEnumerable<RoomNpc>? roomNpcs = null,
        IEnumerable<LocalRuleState>? localRuleStates = null)
    {
        return Rehydrate(
            id,
            depth,
            roomType,
            PalaceRoomState.Neutral,
            theme,
            bossProfile,
            state,
            currentNodeDepth,
            nodes,
            layoutTemplateKey,
            layoutTemplateVersion,
            grid,
            currentGridNodeId,
            roomNpcs,
            localRuleStates);
    }
}
