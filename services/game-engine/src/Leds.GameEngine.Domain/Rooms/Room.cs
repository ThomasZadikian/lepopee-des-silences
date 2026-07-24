using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<MapNode> _nodes;

    /// <summary>
    /// Grid mode only — the node currently in the Select/Resolve interaction slot (set by
    /// <see cref="EnterNodeAtPartyPosition"/>/<see cref="ChallengeBossRemotely"/>). Needed
    /// because, unlike Classic where <see cref="CurrentNodeDepth"/> naturally scopes
    /// "current" to a single row, a grid node stays <see cref="NodeState.Resolved"/> forever
    /// once resolved — so after a second node is resolved, scanning _nodes by state alone
    /// would match more than one node. See CurrentSelectedNode/CurrentResolvedNode below.
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
        RoomGrid? grid = null)
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

    public int CurrentNodeDepth { get; private set; }

    public int MaxNodeDepth { get; }

    public int TotalNodeCount => _nodes.Count;

    public RoomState State { get; private set; }

    public IReadOnlyCollection<MapNode> Nodes => _nodes.AsReadOnly();

    public string? LayoutTemplateKey { get; }

    public string? LayoutTemplateVersion { get; }

    /// <summary>
    /// Free-movement grid overlay — null for every Classic-mode room (row/lane node-graph
    /// behavior is completely untouched). Non-null only for rooms built via <see cref="CreateGrid"/>.
    /// </summary>
    public RoomGrid? Grid { get; }

    /// <summary>Persistence-facing view of <see cref="_currentGridNodeId"/> — see its own doc comment.</summary>
    public NodeId? CurrentGridNodeId => _currentGridNodeId;

    public CatalogRoomBinding? CatalogBinding { get; private set; }

    public void AttachCatalogBinding(CatalogRoomBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        CatalogBinding = binding;
    }

    public IReadOnlyCollection<MapNode> AvailableNodes => Grid is null
        ? _nodes.Where(n => n.Row == CurrentNodeDepth && n.State == NodeState.Available).ToArray()
        : VisibleNodes.Where(n => n.State == NodeState.Available).ToArray();

    /// <summary>
    /// Nodes revealed by fog of war so far — empty for Classic-mode rooms (which have no
    /// fog of war; the full node graph is always sent, gated by lock state instead).
    /// </summary>
    public IReadOnlyCollection<MapNode> VisibleNodes => Grid is null
        ? Array.Empty<MapNode>()
        : _nodes.Where(n => Grid.RevealedNodeIds.Contains(n.Id)).ToArray();

    /// <summary>
    /// The single node currently in <see cref="NodeState.Selected"/>, if any — mode-aware so
    /// Application-layer call sites don't need to know whether this room uses the row/lane DAG
    /// (Classic) or free grid exploration (Tactical).
    /// </summary>
    public MapNode? CurrentSelectedNode => Grid is not null
        ? CurrentGridInteractionNode(NodeState.Selected)
        : _nodes.SingleOrDefault(n => n.State == NodeState.Selected && n.Row == CurrentNodeDepth);

    /// <summary>Mode-aware counterpart of <see cref="CurrentSelectedNode"/> for <see cref="NodeState.Resolved"/>.</summary>
    public MapNode? CurrentResolvedNode => Grid is not null
        ? CurrentGridInteractionNode(NodeState.Resolved)
        : _nodes.SingleOrDefault(n => n.State == NodeState.Resolved && n.Row == CurrentNodeDepth);

    /// <summary>
    /// Grid-mode lookup by the tracked <see cref="_currentGridNodeId"/> instead of scanning
    /// all nodes by state — a resolved grid node never reverts, so more than one node can be
    /// Resolved at once; only the one we most recently selected/resolved is "current".
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
        IEnumerable<MapNode> nodes)
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

        var nodeList = nodes?.ToList()
            ?? throw new DomainException("Room nodes are required.");

        if (nodeList.Count < 2)
        {
            throw new DomainException("A room must contain at least 2 nodes.");
        }

        var maxRow = nodeList.Max(n => n.Row);

        var bossNodes = nodeList.Where(n => n.IsBoss).ToArray();

        if (bossNodes.Length != 1)
        {
            throw new DomainException("A room must contain exactly one boss node.");
        }

        var bossNode = bossNodes.Single();

        if (bossNode.Row != maxRow)
        {
            throw new DomainException("The boss node must be placed at the final row.");
        }

        if (bossNode.State != NodeState.Planned)
        {
            throw new DomainException("The boss node must start as planned.");
        }

        var finalRowNodes = nodeList.Where(n => n.Row == maxRow).ToArray();

        if (finalRowNodes.Length != 1 || !finalRowNodes.Single().IsBoss)
        {
            throw new DomainException("The final row must contain exactly one boss node.");
        }

        var initialNodes = nodeList.Where(n => n.Row == 0).ToArray();

        if (initialNodes.Length < 1)
        {
            throw new DomainException("Initial row must contain at least 1 node.");
        }

        if (initialNodes.Any(n => n.State != NodeState.Available))
        {
            throw new DomainException("Initial row nodes must start as available.");
        }

        var futureNodes = nodeList.Where(n => n.Row > 0).ToArray();

        if (futureNodes.Any(n => n.State != NodeState.Planned))
        {
            throw new DomainException("Future row nodes must start as planned.");
        }

        EnsureRowsAreContinuous(nodeList, maxRow);
        EnsureParentReferencesAreValid(nodeList);
        EnsureAllNonBossNodesHaveChildren(nodeList, maxRow);
        EnsureAllPathsConvergeToBoss(nodeList, bossNode);
        EnsureNoCrossRowConnections(nodeList);

        return new Room(
            RoomId.New(),
            depth,
            roomType,
            palaceState,
            theme.Trim(),
            bossProfile,
            RoomState.Active,
            nodeList,
            layoutTemplateKey: null,
            layoutTemplateVersion: null);
    }

    public static Room CreateFromTemplate(
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes,
        string layoutTemplateKey,
        string layoutTemplateVersion)
    {
        var room = Create(depth, roomType, palaceState, theme, bossProfile, nodes);

        if (string.IsNullOrWhiteSpace(layoutTemplateKey))
        {
            throw new DomainException("Layout template key is required.");
        }

        if (string.IsNullOrWhiteSpace(layoutTemplateVersion))
        {
            throw new DomainException("Layout template version is required.");
        }

        return new Room(
            room.Id,
            room.Depth,
            room.RoomType,
            room.PalaceState,
            room.Theme,
            room.BossProfile,
            room.State,
            room._nodes,
            layoutTemplateKey.Trim(),
            layoutTemplateVersion.Trim());
    }

    /// <summary>
    /// Builds a Tactical-mode room: nodes are placed on a free-movement grid (Row=Y, Lane=X)
    /// instead of the Classic row-by-row DAG. Deliberately does NOT call any of the DAG-only
    /// validations (<see cref="EnsureRowsAreContinuous"/>, <see cref="EnsureParentReferencesAreValid"/>,
    /// <see cref="EnsureAllNonBossNodesHaveChildren"/>, <see cref="EnsureAllPathsConvergeToBoss"/>,
    /// <see cref="EnsureNoCrossRowConnections"/>) — none of them apply to free exploration.
    /// </summary>
    public static Room CreateGrid(
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
        string layoutTemplateVersion)
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

        var distanceToBoss = Math.Abs(bossNode.Lane - startX) + Math.Abs(bossNode.Row - startY);

        if (distanceToBoss > movementBudget)
        {
            throw new DomainException(
                "The boss must be reachable within the room's movement budget.");
        }

        var grid = RoomGrid.CreateInitial(gridWidth, gridHeight, movementBudget, startX, startY, nodeList);

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

    public MapNode GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    public void SelectNode(NodeId nodeId)
    {
        EnsureClassicRoom();

        if (State is not RoomState.Active and not RoomState.BossReached)
        {
            throw new DomainException("Room is not waiting for a node selection.");
        }

        var selectedNode = GetNode(nodeId);

        if (selectedNode.Row != CurrentNodeDepth)
        {
            throw new DomainException("Only a node from the current room depth can be selected.");
        }

        selectedNode.Select();

        foreach (var node in _nodes.Where(n =>
                     n.Row == CurrentNodeDepth && n.Id != nodeId))
        {
            node.Lock();
        }

        MarkUnreachableBranches(selectedNode);

        State = RoomState.NodeSelected;
    }

    public void ResolveSelectedNodeEvent()
    {
        EnsureClassicRoom();

        if (State != RoomState.NodeSelected)
        {
            throw new DomainException("Room must have a selected node before resolving an event.");
        }

        var selectedNode = CurrentSelectedNode
            ?? throw new DomainException("No node has been selected for the current room depth.");

        selectedNode.Resolve();

        State = selectedNode.IsBoss
            ? RoomState.Completed
            : RoomState.NodeResolved;
    }

    public void UnlockNextNodeLayer()
    {
        EnsureClassicRoom();

        if (State != RoomState.NodeResolved)
        {
            throw new DomainException("Current node event must be resolved before progressing.");
        }

        if (CurrentNodeDepth >= MaxNodeDepth)
        {
            throw new DomainException("Room has already reached its final node depth.");
        }

        var resolvedNode = CurrentResolvedNode
            ?? throw new DomainException("No node has been resolved at the current room depth.");

        var nextDepth = CurrentNodeDepth + 1;

        var nextLayerNodes = _nodes
            .Where(n =>
                n.Row == nextDepth &&
                n.State == NodeState.Planned &&
                n.ParentNodeIds.Contains(resolvedNode.Id))
            .ToArray();

        if (nextLayerNodes.Length == 0)
        {
            throw new DomainException("Resolved node has no reachable child in the next node layer.");
        }

        foreach (var node in nextLayerNodes)
        {
            node.Unlock();
        }

        CurrentNodeDepth = nextDepth;

        State = nextLayerNodes.Any(n => n.IsBoss)
            ? RoomState.BossReached
            : RoomState.Active;
    }

    /// <summary>
    /// Tactical-mode counterpart of the Classic <see cref="SelectNode"/>/
    /// <see cref="UnlockNextNodeLayer"/> pair — moves the party across the grid, deducting the
    /// Manhattan-distance cost from the movement budget and revealing fog of war along the path.
    /// </summary>
    public void MoveParty(int targetX, int targetY)
    {
        EnsureGridRoom();

        if (State is not RoomState.Active)
        {
            throw new DomainException("Room is not waiting for party movement.");
        }

        if (targetX < 0 || targetX >= Grid!.Width || targetY < 0 || targetY >= Grid.Height)
        {
            throw new DomainException("Target position is outside the grid bounds.");
        }

        var cost = Math.Abs(targetX - Grid.PartyX) + Math.Abs(targetY - Grid.PartyY);

        if (cost == 0)
        {
            throw new DomainException("The party is already at the target position.");
        }

        if (cost > Grid.MovementBudgetRemaining)
        {
            throw new DomainException("Not enough movement budget remaining for this move.");
        }

        Grid.MoveTo(targetX, targetY, cost, _nodes);
    }

    /// <summary>
    /// Selects the node currently occupied by the party — the grid-mode equivalent of
    /// <see cref="SelectNode"/> (which instead requires the node to be on the current row).
    /// </summary>
    public void EnterNodeAtPartyPosition(NodeId nodeId)
    {
        EnsureGridRoom();

        if (State is not RoomState.Active)
        {
            throw new DomainException("Room is not waiting for a node selection.");
        }

        var node = GetNode(nodeId);

        if (node.Lane != Grid!.PartyX || node.Row != Grid.PartyY)
        {
            throw new DomainException("The party is not standing on this node's cell.");
        }

        node.Select();
        _currentGridNodeId = nodeId;
        State = RoomState.NodeSelected;
    }

    /// <summary>Grid-mode counterpart of <see cref="ResolveSelectedNodeEvent"/>.</summary>
    public void ResolveSelectedGridNodeEvent()
    {
        EnsureGridRoom();

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

    /// <summary>
    /// Grid-mode counterpart of <see cref="UnlockNextNodeLayer"/> — there is no next layer to
    /// unlock in free exploration, so this simply returns the room to free movement.
    /// </summary>
    public void ReturnToGridExploration()
    {
        EnsureGridRoom();

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
        Grid is not null
        && Grid.MovementBudgetRemaining <= 0
        && State is RoomState.Active
        && _nodes.Single(n => n.IsBoss).State == NodeState.Available;

    /// <summary>
    /// "Le boss approche à grands pas" — lets the player engage the boss directly once movement
    /// budget is exhausted, without needing to be on its cell. Reuses the exact same
    /// <see cref="MapNode.Select"/>/<see cref="ResolveSelectedGridNodeEvent"/> path as walking
    /// onto the boss's cell normally would.
    /// </summary>
    public void ChallengeBossRemotely()
    {
        EnsureGridRoom();

        if (!CanChallengeBossRemotely)
        {
            throw new DomainException("Remote boss challenge is not available yet.");
        }

        var bossNode = _nodes.Single(n => n.IsBoss);
        bossNode.Select();
        _currentGridNodeId = bossNode.Id;
        State = RoomState.NodeSelected;
    }

    private void EnsureGridRoom()
    {
        if (Grid is null)
        {
            throw new DomainException("Room is not a grid room.");
        }
    }

    private void EnsureClassicRoom()
    {
        if (Grid is not null)
        {
            throw new DomainException("Room is a grid room; use the Tactical-mode methods instead.");
        }
    }

    public void ResetProgress()
    {
        if (State is RoomState.Active or RoomState.NodeSelected
            or RoomState.NodeResolved or RoomState.BossReached)
        {
            if (Grid is not null)
            {
                foreach (var node in _nodes)
                {
                    node.ResetToGridAvailable();
                }

                Grid.ResetToInitial(_nodes);
                State = RoomState.Active;
                return;
            }

            foreach (var node in _nodes)
            {
                node.ResetToInitial();
            }

            CurrentNodeDepth = 0;
            State = RoomState.Active;
            return;
        }

        throw new DomainException("Room is closed and cannot be reset.");
    }

    private void MarkUnreachableBranches(MapNode selectedNode)
    {
        foreach (var node in _nodes.Where(n => n.Row > selectedNode.Row))
        {
            if (!IsReachableFrom(selectedNode.Id, node))
            {
                node.MarkUnreachable();
            }
        }
    }

    private bool IsReachableFrom(NodeId ancestorNodeId, MapNode node)
    {
        if (node.ParentNodeIds.Contains(ancestorNodeId))
        {
            return true;
        }

        foreach (var parentNodeId in node.ParentNodeIds)
        {
            var parent = _nodes.FirstOrDefault(candidate => candidate.Id == parentNodeId);

            if (parent is not null && IsReachableFrom(ancestorNodeId, parent))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureRowsAreContinuous(
        IReadOnlyCollection<MapNode> nodes, int maxRow)
    {
        var rows = nodes
            .Select(n => n.Row)
            .Distinct()
            .Order()
            .ToArray();

        var expectedRows = Enumerable.Range(0, maxRow + 1).ToArray();

        if (!rows.SequenceEqual(expectedRows))
        {
            throw new DomainException("Room rows must be continuous from 0 to the boss row.");
        }
    }

    private static void EnsureParentReferencesAreValid(IReadOnlyCollection<MapNode> nodes)
    {
        var nodesById = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            if (node.Row == 0 && node.ParentNodeIds.Count != 0)
            {
                throw new DomainException("Initial row nodes cannot have parents.");
            }

            foreach (var parentNodeId in node.ParentNodeIds)
            {
                if (!nodesById.TryGetValue(parentNodeId, out var parent))
                {
                    throw new DomainException("Node parent reference does not belong to this room.");
                }

                if (parent.Row != node.Row - 1)
                {
                    throw new DomainException("Node parents must belong to the previous row.");
                }

                if (parent.IsBoss)
                {
                    throw new DomainException("Boss node cannot be used as a parent.");
                }
            }
        }
    }

    private static void EnsureAllNonBossNodesHaveChildren(
        IReadOnlyCollection<MapNode> nodes, int maxRow)
    {
        foreach (var node in nodes.Where(n => !n.IsBoss))
        {
            if (node.Row >= maxRow)
            {
                throw new DomainException("Only the boss node can exist at the final row.");
            }

            var hasChild = nodes.Any(candidate =>
                candidate.Row == node.Row + 1 &&
                candidate.ParentNodeIds.Contains(node.Id));

            if (!hasChild)
            {
                throw new DomainException("Every non-boss node must have at least one child.");
            }
        }
    }

    private static void EnsureAllPathsConvergeToBoss(
        IReadOnlyCollection<MapNode> nodes, MapNode bossNode)
    {
        foreach (var node in nodes.Where(n => !n.IsBoss))
        {
            if (!HasPathToBoss(node, bossNode, nodes))
            {
                throw new DomainException("Every node must have a valid path to the boss.");
            }
        }
    }

    private static bool HasPathToBoss(
        MapNode currentNode, MapNode bossNode, IReadOnlyCollection<MapNode> nodes)
    {
        var children = nodes
            .Where(n => n.ParentNodeIds.Contains(currentNode.Id))
            .ToArray();

        if (children.Any(c => c.Id == bossNode.Id))
        {
            return true;
        }

        return children.Any(c => HasPathToBoss(c, bossNode, nodes));
    }

    private static void EnsureNoCrossRowConnections(IReadOnlyCollection<MapNode> nodes)
    {
        var nodesById = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            foreach (var parentNodeId in node.ParentNodeIds)
            {
                if (!nodesById.TryGetValue(parentNodeId, out var parent))
                {
                    continue;
                }

                if (Math.Abs(parent.Lane - node.Lane) > 1)
                {
                    throw new DomainException(
                        "Node connections must be between same or adjacent lanes only.");
                }
            }
        }
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
        RoomGrid? grid = null,
        NodeId? currentGridNodeId = null)
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

    public static Room Create(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes)
    {
        return Create(depth, roomType, PalaceRoomState.Neutral, theme, bossProfile, nodes);
    }

    public static Room CreateFromTemplate(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes,
        string layoutTemplateKey,
        string layoutTemplateVersion)
    {
        return CreateFromTemplate(
            depth,
            roomType,
            PalaceRoomState.Neutral,
            theme,
            bossProfile,
            nodes,
            layoutTemplateKey,
            layoutTemplateVersion);
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
        RoomGrid? grid = null,
        NodeId? currentGridNodeId = null)
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
