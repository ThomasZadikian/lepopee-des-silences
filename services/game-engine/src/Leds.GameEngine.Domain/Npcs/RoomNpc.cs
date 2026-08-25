using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Actors;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// A physically present, positioned NPC in a room's exploration grid — distinct from
/// <see cref="NpcRelationship"/> (the run-scoped dialogue/relationship state carried across the
/// whole Palais). A <see cref="RoomNpc"/> is this room INSTANCE's physical presence: where it
/// stands, how it moves, and whether it has noticed the party yet. Contrat canonique §38-41,
/// SFD Hall d'entrée §IV ("un PNJ peut être présent, actif et en interaction avec son
/// environnement sans avoir encore conscience du groupe").
/// <para>
/// Deliberately independent of <see cref="Nodes.MapNode"/>: a MapNode's state machine
/// (Planned→Available→Selected→Resolved) models event RESOLUTION, not spatial simulation, and
/// its position is immutable for the room's lifetime — neither fits a PNJ that walks around.
/// The two systems meet only through <see cref="CatalogNpcKey"/>, which the Application layer
/// can join against a NpcRelationship/dialogue graph when this NPC is actually approached.
/// </para>
/// </summary>
public sealed class RoomNpc : IActorInstance
{
    // BALANCE KNOB — how close the party must be (Manhattan distance) before an Unaware NPC can
    // notice it at all. Line of sight (see RefreshAwareness) still gates the actual transition.
    public const int DefaultAwarenessRadius = 4;

    // BALANCE KNOB — how far a Passive NPC will stray from its spawn cell before turning back.
    // Keeps ambient wander a local wobble, not a cross-room walk.
    public const int WanderLeashRadius = 3;

    private static readonly (int Dx, int Dy)[] OrthogonalDirections =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1),
    };

    private readonly List<(int X, int Y)> _waypoints;
    private int _waypointIndex;
    private int _stepCount;

    private RoomNpc(
        RoomNpcId id,
        string catalogNpcKey,
        int originX,
        int originY,
        int x,
        int y,
        NpcBehaviorArchetype behavior,
        NpcAwarenessState awareness,
        int awarenessRadius,
        List<(int X, int Y)> waypoints,
        int waypointIndex,
        int stepCount)
    {
        Id = id;
        CatalogNpcKey = catalogNpcKey;
        OriginX = originX;
        OriginY = originY;
        X = x;
        Y = y;
        Behavior = behavior;
        Awareness = awareness;
        AwarenessRadius = awarenessRadius;
        _waypoints = waypoints;
        _waypointIndex = waypointIndex;
        _stepCount = stepCount;
    }

    public RoomNpcId Id { get; }

    public Guid ActorInstanceId => Id.Value;

    public string ActorDefinitionKey => CatalogNpcKey;

    public ActorKind ActorKind => global::Leds.GameEngine.Domain.Actors.ActorKind.Npc;

    /// <summary>Catalog NPC this presence embodies — joins to dialogue/relationship state, never
    /// held by reference (see class remarks).</summary>
    public string CatalogNpcKey { get; }

    /// <summary>Spawn cell — the anchor a Guardian holds and a Passive/Hunter-at-rest wanders
    /// around or returns to.</summary>
    public int OriginX { get; }

    public int OriginY { get; }

    public int X { get; private set; }

    public int Y { get; private set; }

    public NpcBehaviorArchetype Behavior { get; }

    public NpcAwarenessState Awareness { get; private set; }

    /// <summary>0 means this NPC never notices the party on its own — the mechanism behind the
    /// SFD's "certaines entités peuvent ne pas percevoir le joueur" (two habitants absorbed in
    /// their own conversation), without a second field to keep in sync.</summary>
    public int AwarenessRadius { get; }

    /// <summary>Authored route for <see cref="NpcBehaviorArchetype.Patrol"/>; empty for every
    /// other archetype.</summary>
    public IReadOnlyList<(int X, int Y)> Waypoints => _waypoints;

    /// <summary>Index into <see cref="Waypoints"/> this NPC is currently walking toward —
    /// persisted so a reloaded patrol resumes mid-route instead of restarting it.</summary>
    public int WaypointIndex => _waypointIndex;

    /// <summary>Monotonic step counter feeding the deterministic wander hash in
    /// <see cref="StepIdle"/> — persisted so a reloaded NPC's wander stays on the same
    /// deterministic sequence instead of restarting it.</summary>
    public int StepCount => _stepCount;

    public static RoomNpc Create(
        string catalogNpcKey,
        int x,
        int y,
        NpcBehaviorArchetype behavior,
        int awarenessRadius = DefaultAwarenessRadius,
        IReadOnlyList<(int X, int Y)>? waypoints = null)
    {
        if (string.IsNullOrWhiteSpace(catalogNpcKey))
        {
            throw new DomainException("Room NPC catalog key is required.");
        }

        if (x < 0 || y < 0)
        {
            throw new DomainException("Room NPC position must be within the grid.");
        }

        if (awarenessRadius < 0)
        {
            throw new DomainException("Room NPC awareness radius must be greater than or equal to 0.");
        }

        var waypointList = waypoints?.ToList() ?? new List<(int X, int Y)>();

        if (behavior == NpcBehaviorArchetype.Patrol && waypointList.Count == 0)
        {
            throw new DomainException("A patrolling NPC needs at least one waypoint.");
        }

        if (behavior != NpcBehaviorArchetype.Patrol && waypointList.Count > 0)
        {
            throw new DomainException("Only a patrolling NPC can carry waypoints.");
        }

        return new RoomNpc(
            RoomNpcId.New(), catalogNpcKey.Trim(), x, y, x, y, behavior,
            NpcAwarenessState.Unaware, awarenessRadius, waypointList, waypointIndex: 0, stepCount: 0);
    }

    public static RoomNpc Rehydrate(
        RoomNpcId id,
        string catalogNpcKey,
        int originX,
        int originY,
        int x,
        int y,
        NpcBehaviorArchetype behavior,
        NpcAwarenessState awareness,
        int awarenessRadius,
        IReadOnlyList<(int X, int Y)> waypoints,
        int waypointIndex,
        int stepCount)
    {
        return new RoomNpc(
            id, catalogNpcKey, originX, originY, x, y, behavior, awareness, awarenessRadius,
            waypoints.ToList(), waypointIndex, stepCount);
    }

    /// <summary>
    /// Advances this NPC by exactly one cell of simulation — never more, matching the party's
    /// own one-step-at-a-time movement (Contrat canonique §13, "chaque case réellement
    /// parcourue constitue un pas déterministe"). Call once per party step, after the party's
    /// move has landed, so a Hunter always reacts to where the party actually is.
    /// </summary>
    public void Step(RoomGrid grid, int partyX, int partyY)
    {
        _stepCount++;

        switch (Behavior)
        {
            case NpcBehaviorArchetype.Fixed:
            case NpcBehaviorArchetype.Guardian:
                // A guardian holds its post — it is the transgression/event system (not
                // movement) that turns "watching" into "closing in", by re-tagging its
                // Awareness/authored reaction, not by relocating it here.
                return;
            case NpcBehaviorArchetype.Patrol:
                StepAlongPatrol(grid);
                return;
            case NpcBehaviorArchetype.Hunter:
                if (Awareness >= NpcAwarenessState.Aware)
                {
                    StepToward(grid, partyX, partyY);
                }
                return;
            case NpcBehaviorArchetype.Passive:
                StepIdle(grid);
                return;
        }
    }

    /// <summary>
    /// Proximity + line of sight only ever ESCALATE an Unaware NPC to Aware — never the reverse,
    /// and never past Aware. Alert is reserved for an explicit trigger (<see cref="RaiseAlert"/>:
    /// interaction, event, or transgression), matching the Contrat's own causal list — ordinary
    /// proximity is not itself a transgression.
    /// </summary>
    public void RefreshAwareness(RoomGrid grid, int partyX, int partyY)
    {
        if (Awareness != NpcAwarenessState.Unaware || AwarenessRadius <= 0)
        {
            return;
        }

        var distance = Math.Abs(X - partyX) + Math.Abs(Y - partyY);
        if (distance <= AwarenessRadius && grid.HasLineOfSight(X, Y, partyX, partyY))
        {
            Awareness = NpcAwarenessState.Aware;
        }
    }

    /// <summary>A direct interaction always registers the party, regardless of range/sightline.</summary>
    public void NoticeParty()
    {
        if (Awareness == NpcAwarenessState.Unaware)
        {
            Awareness = NpcAwarenessState.Aware;
        }
    }

    /// <summary>An event or transgression escalates straight to Alert — see Ensemble 2's
    /// protocole/transgression engine, the intended caller.</summary>
    public void RaiseAlert() => Awareness = NpcAwarenessState.Alert;

    /// <summary>De-escalates once whatever raised the alert has been resolved.</summary>
    public void Calm()
    {
        if (Awareness == NpcAwarenessState.Alert)
        {
            Awareness = NpcAwarenessState.Aware;
        }
    }

    private void StepAlongPatrol(RoomGrid grid)
    {
        if (_waypoints.Count == 0)
        {
            return;
        }

        var (targetX, targetY) = _waypoints[_waypointIndex];

        if (X == targetX && Y == targetY)
        {
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Count;
            (targetX, targetY) = _waypoints[_waypointIndex];
        }

        StepToward(grid, targetX, targetY);
    }

    private void StepIdle(RoomGrid grid)
    {
        if (Math.Abs(X - OriginX) + Math.Abs(Y - OriginY) >= WanderLeashRadius)
        {
            StepToward(grid, OriginX, OriginY);
            return;
        }

        // Deterministic pseudo-wander: the same NPC id at the same step count always picks the
        // same direction, so a replayed run reproduces identically without threading a live RNG
        // through gameplay-time movement — only generation itself is randomized (Contrat
        // canonique §102).
        var directionSeed = unchecked((Id.Value.GetHashCode() * 397) ^ _stepCount);
        var direction = OrthogonalDirections[((directionSeed % OrthogonalDirections.Length) + OrthogonalDirections.Length) % OrthogonalDirections.Length];
        TryMove(grid, X + direction.Dx, Y + direction.Dy);
    }

    private void StepToward(RoomGrid grid, int targetX, int targetY)
    {
        var dx = Math.Sign(targetX - X);
        var dy = Math.Sign(targetY - Y);
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var horizontalFirst = Math.Abs(targetX - X) >= Math.Abs(targetY - Y);

        if (horizontalFirst)
        {
            if (dx != 0 && TryMove(grid, X + dx, Y))
            {
                return;
            }

            if (dy != 0)
            {
                TryMove(grid, X, Y + dy);
            }
        }
        else
        {
            if (dy != 0 && TryMove(grid, X, Y + dy))
            {
                return;
            }

            if (dx != 0)
            {
                TryMove(grid, X + dx, Y);
            }
        }
    }

    private bool TryMove(RoomGrid grid, int x, int y)
    {
        if (!grid.IsWalkable(x, y))
        {
            return false;
        }

        X = x;
        Y = y;
        return true;
    }
}
