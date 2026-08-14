using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Npcs;

public sealed class RoomNpcTests
{
    private static RoomGrid CreateOpenGrid(int width = 12, int height = 12) =>
        RoomGrid.CreateInitial(width, height, movementBudget: 20, startX: 0, startY: 0, nodes: []);

    [Fact]
    public void Create_ShouldRejectEmptyCatalogKey()
    {
        var act = () => RoomNpc.Create(" ", x: 0, y: 0, NpcBehaviorArchetype.Fixed);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectNegativePosition()
    {
        var act = () => RoomNpc.Create("majordome", x: -1, y: 0, NpcBehaviorArchetype.Fixed);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectNegativeAwarenessRadius()
    {
        var act = () => RoomNpc.Create("majordome", x: 0, y: 0, NpcBehaviorArchetype.Fixed, awarenessRadius: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_PatrolWithoutWaypoints_ShouldThrow()
    {
        var act = () => RoomNpc.Create("garde", x: 0, y: 0, NpcBehaviorArchetype.Patrol);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NonPatrolWithWaypoints_ShouldThrow()
    {
        var act = () => RoomNpc.Create(
            "garde", x: 0, y: 0, NpcBehaviorArchetype.Fixed, waypoints: [(1, 1)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldStartUnaware_AtGivenOriginAndPosition()
    {
        var npc = RoomNpc.Create("majordome", x: 3, y: 4, NpcBehaviorArchetype.Guardian);

        npc.Awareness.Should().Be(NpcAwarenessState.Unaware);
        npc.X.Should().Be(3);
        npc.Y.Should().Be(4);
        npc.OriginX.Should().Be(3);
        npc.OriginY.Should().Be(4);
    }

    [Fact]
    public void RefreshAwareness_ShouldEscalateToAware_WhenPartyWithinRadiusAndLineOfSight()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("majordome", x: 5, y: 5, NpcBehaviorArchetype.Guardian, awarenessRadius: 4);

        npc.RefreshAwareness(grid, partyX: 6, partyY: 5);

        npc.Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void RefreshAwareness_ShouldStayUnaware_WhenPartyOutsideRadius()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("majordome", x: 0, y: 0, NpcBehaviorArchetype.Guardian, awarenessRadius: 2);

        npc.RefreshAwareness(grid, partyX: 11, partyY: 11);

        npc.Awareness.Should().Be(NpcAwarenessState.Unaware);
    }

    [Fact]
    public void RefreshAwareness_ShouldNeverNotice_WhenAwarenessRadiusIsZero()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("habitant", x: 5, y: 5, NpcBehaviorArchetype.Passive, awarenessRadius: 0);

        npc.RefreshAwareness(grid, partyX: 5, partyY: 5);

        npc.Awareness.Should().Be(NpcAwarenessState.Unaware);
    }

    [Fact]
    public void RefreshAwareness_ShouldNeverDeescalate_OnceAware()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("majordome", x: 5, y: 5, NpcBehaviorArchetype.Guardian, awarenessRadius: 4);
        npc.RefreshAwareness(grid, partyX: 6, partyY: 5);
        npc.Awareness.Should().Be(NpcAwarenessState.Aware);

        npc.RefreshAwareness(grid, partyX: 11, partyY: 11);

        npc.Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void NoticeParty_ShouldEscalateUnawareToAware_RegardlessOfRange()
    {
        var npc = RoomNpc.Create("majordome", x: 0, y: 0, NpcBehaviorArchetype.Guardian, awarenessRadius: 0);

        npc.NoticeParty();

        npc.Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void RaiseAlert_ThenCalm_ShouldReturnToAware_NotUnaware()
    {
        var npc = RoomNpc.Create("majordome", x: 0, y: 0, NpcBehaviorArchetype.Guardian);
        npc.RaiseAlert();
        npc.Awareness.Should().Be(NpcAwarenessState.Alert);

        npc.Calm();

        npc.Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void Step_Fixed_ShouldNeverMove()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("statue", x: 5, y: 5, NpcBehaviorArchetype.Fixed);

        for (var i = 0; i < 10; i++)
        {
            npc.Step(grid, partyX: 5, partyY: 6);
        }

        npc.X.Should().Be(5);
        npc.Y.Should().Be(5);
    }

    [Fact]
    public void Step_Guardian_ShouldHoldPost_EvenWhenAware()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("garde", x: 5, y: 5, NpcBehaviorArchetype.Guardian, awarenessRadius: 10);
        npc.RefreshAwareness(grid, partyX: 5, partyY: 6);
        npc.Awareness.Should().Be(NpcAwarenessState.Aware);

        npc.Step(grid, partyX: 5, partyY: 6);

        npc.X.Should().Be(5);
        npc.Y.Should().Be(5);
    }

    [Fact]
    public void Step_Hunter_ShouldNotMove_WhileUnaware()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("chasseur", x: 5, y: 5, NpcBehaviorArchetype.Hunter, awarenessRadius: 0);

        npc.Step(grid, partyX: 0, partyY: 0);

        npc.X.Should().Be(5);
        npc.Y.Should().Be(5);
    }

    [Fact]
    public void Step_Hunter_ShouldCloseDistance_OnceAware()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("chasseur", x: 5, y: 5, NpcBehaviorArchetype.Hunter);
        npc.NoticeParty();

        npc.Step(grid, partyX: 0, partyY: 0);

        (npc.X + npc.Y).Should().BeLessThan(10);
    }

    [Fact]
    public void Step_Patrol_ShouldCycleWaypoints_InOrder()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create(
            "sentinelle", x: 0, y: 0, NpcBehaviorArchetype.Patrol,
            waypoints: [(2, 0), (2, 2)]);

        for (var i = 0; i < 2; i++)
        {
            npc.Step(grid, partyX: 11, partyY: 11);
        }

        npc.X.Should().Be(2);
        npc.Y.Should().Be(0);

        // Reaching a waypoint advances to the next one and starts moving toward it within the
        // very same Step call, so two more steps land exactly on the second waypoint.
        for (var i = 0; i < 2; i++)
        {
            npc.Step(grid, partyX: 11, partyY: 11);
        }

        npc.X.Should().Be(2);
        npc.Y.Should().Be(2);
    }

    [Fact]
    public void Step_Passive_ShouldStayWithinLeashRadius_OfOrigin()
    {
        var grid = CreateOpenGrid();
        var npc = RoomNpc.Create("habitant", x: 6, y: 6, NpcBehaviorArchetype.Passive);

        for (var i = 0; i < 50; i++)
        {
            npc.Step(grid, partyX: 0, partyY: 0);
            (Math.Abs(npc.X - 6) + Math.Abs(npc.Y - 6)).Should().BeLessThanOrEqualTo(RoomNpc.WanderLeashRadius);
        }
    }

    [Fact]
    public void Step_Passive_ShouldBeDeterministic_ForTheSameIdAndStepSequence()
    {
        var grid = CreateOpenGrid();
        var original = RoomNpc.Create("habitant", x: 6, y: 6, NpcBehaviorArchetype.Passive);

        // Rehydrate a second instance with the exact same id/state, mirroring a save/reload —
        // determinism must hold across the persistence boundary, not just within one instance.
        var replay = RoomNpc.Rehydrate(
            original.Id, original.CatalogNpcKey, original.OriginX, original.OriginY,
            original.X, original.Y, original.Behavior, original.Awareness, original.AwarenessRadius,
            original.Waypoints, original.WaypointIndex, original.StepCount);

        for (var i = 0; i < 30; i++)
        {
            original.Step(grid, partyX: 0, partyY: 0);
            replay.Step(grid, partyX: 0, partyY: 0);

            replay.X.Should().Be(original.X);
            replay.Y.Should().Be(original.Y);
        }
    }

    [Fact]
    public void Rehydrate_ShouldPreserveAllFields()
    {
        var npc = RoomNpc.Create(
            "sentinelle", x: 1, y: 2, NpcBehaviorArchetype.Patrol, awarenessRadius: 3,
            waypoints: [(4, 5), (6, 7)]);
        npc.NoticeParty();
        npc.RaiseAlert();
        npc.Step(CreateOpenGrid(), partyX: 0, partyY: 0);

        var rehydrated = RoomNpc.Rehydrate(
            npc.Id, npc.CatalogNpcKey, npc.OriginX, npc.OriginY, npc.X, npc.Y,
            npc.Behavior, npc.Awareness, npc.AwarenessRadius, npc.Waypoints,
            npc.WaypointIndex, npc.StepCount);

        rehydrated.Id.Should().Be(npc.Id);
        rehydrated.CatalogNpcKey.Should().Be(npc.CatalogNpcKey);
        rehydrated.OriginX.Should().Be(npc.OriginX);
        rehydrated.OriginY.Should().Be(npc.OriginY);
        rehydrated.X.Should().Be(npc.X);
        rehydrated.Y.Should().Be(npc.Y);
        rehydrated.Behavior.Should().Be(npc.Behavior);
        rehydrated.Awareness.Should().Be(npc.Awareness);
        rehydrated.AwarenessRadius.Should().Be(npc.AwarenessRadius);
        rehydrated.Waypoints.Should().Equal(npc.Waypoints);
        rehydrated.WaypointIndex.Should().Be(npc.WaypointIndex);
        rehydrated.StepCount.Should().Be(npc.StepCount);
    }
}
