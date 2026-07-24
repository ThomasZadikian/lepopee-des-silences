using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<MapNode> _nodes;

    /// <summary>
    /// The node currently in the Select/Resolve interaction slot (set by
    /// <see cref="EnterNodeAtPartyPosition"/>/<see cref="ChallengeBossRemotely"/>). Needed
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
        RoomBossProfile bossProfile,
        RoomState state,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid)
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
    }


    public RoomId Id { get; }

    public int Depth { get; }

    public RoomType RoomType { get; }

    public PalaceRoomState PalaceState { get; private set; }

    public string Theme { get; }

    public RoomBossProfile BossProfile { get; }

    /// <summary>
    /// Inert vestige of the Classic row/lane DAG's depth cursor — no longer advanced by
    /// anything (grid rooms never had a meaningful "current row"), but left in place rather
    /// than ripped out of persistence/DTOs for a field with no functional cost to keep.
    /// </summary>
    public int CurrentNodeDepth { get; private set; }

    public int MaxNodeDepth { get; }

    public int TotalNodeCount => _nodes.Count;

    public RoomState State { get; private set; }

    public IReadOnlyCollection<MapNode> Nodes => _nodes.AsReadOnly();

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

    /// <summary>Nodes revealed by fog of war so far.</summary>
    public IReadOnlyCollection<MapNode> VisibleNodes =>
        _nodes.Where(n => Grid.RevealedNodeIds.Contains(n.Id)).ToArray();

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
        RoomBossProfile bossProfile,
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
        if (depth is < 0 or > 10)
        {
            throw new DomainException("Room depth must be between 0 and 10.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new DomainException("Room theme is required.");
        }

        ArgumentNullException.ThrowIfNull(bossProfile);

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

        if (bossNodes.Length != 1)
        {
            throw new DomainException("A room must contain exactly one boss node.");
        }

        var bossNode = bossNodes.Single();

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
            gridWidth, gridHeight, movementBudget, startX, startY, nodeList, elevation, obstacles);

        // Raw Manhattan distance is no longer a safe reachability bound once elevation can add
        // cost to a climb — the real routed cost (obstacles routed around, elevation priced in)
        // is what must fit the movement budget.
        var bossRoute = grid.FindPath(bossNode.Lane, bossNode.Row);

        if (bossRoute is null || bossRoute.Value.Cost > movementBudget)
        {
            throw new DomainException(
                "The boss must be reachable within the room's movement budget.");
        }

        // Defense-in-depth: generation is expected to guarantee every node is reachable from the
        // start (obstacle placement is connectivity-checked at generation time), but a room built
        // by hand (tests, future authoring tools) gets the same guarantee enforced here.
        foreach (var node in nodeList)
        {
            if (!node.IsBoss && grid.FindPath(node.Lane, node.Row) is null)
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
        RoomBossProfile bossProfile,
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
    /// Moves the party across the grid along the cheapest walkable route (obstacles routed
    /// around, elevation climbs priced in — see <see cref="RoomGrid.FindPath"/>), deducting the
    /// route's cost from the movement budget and revealing fog of war along the way.
    /// </summary>
    public void MoveParty(int targetX, int targetY)
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

        var route = Grid.FindPath(targetX, targetY)
            ?? throw new DomainException("No walkable path to the target position.");

        if (route.Cost > Grid.MovementBudgetRemaining)
        {
            throw new DomainException("Not enough movement budget remaining for this move.");
        }

        Grid.MoveTo(route.Path, route.Cost, _nodes);
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

        State = selectedNode.IsBoss
            ? RoomState.Completed
            : RoomState.NodeResolved;
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

    /// <summary>
    /// True once the party has exhausted its movement budget and the boss node hasn't been
    /// engaged yet — the player can then challenge the boss without walking to its cell
    /// (see <see cref="ChallengeBossRemotely"/>), so a room can never become permanently
    /// unfinishable even if the boss ends up unreachable on foot.
    /// </summary>
    public bool CanChallengeBossRemotely =>
        Grid.MovementBudgetRemaining <= 0
        && State is RoomState.Active
        && _nodes.Single(n => n.IsBoss).State == NodeState.Available;

    /// <summary>
    /// "Le boss approche à grands pas" — lets the player engage the boss directly once movement
    /// budget is exhausted, without needing to be on its cell. Reuses the exact same
    /// <see cref="MapNode.Select"/>/<see cref="ResolveSelectedNodeEvent"/> path as walking onto
    /// the boss's cell normally would.
    /// </summary>
    public void ChallengeBossRemotely()
    {
        if (!CanChallengeBossRemotely)
        {
            throw new DomainException("Remote boss challenge is not available yet.");
        }

        var bossNode = _nodes.Single(n => n.IsBoss);
        bossNode.Select();
        _currentGridNodeId = bossNode.Id;
        State = RoomState.NodeSelected;
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
        RoomBossProfile bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid,
        NodeId? currentGridNodeId)
    {
        var room = new Room(id, depth, roomType, palaceState, theme, bossProfile, state, nodes, layoutTemplateKey, layoutTemplateVersion, grid);
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
        RoomBossProfile bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion,
        RoomGrid grid,
        NodeId? currentGridNodeId)
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
            currentGridNodeId);
    }
}
