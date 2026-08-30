using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// PR 0.1.10 — Verifies that <see cref="Run.CurrentRoomIndex"/> tracks the player's
/// position in the infinite room sequence (0-based), independently of
/// <see cref="Room.Depth"/> and <see cref="Room.CurrentNodeDepth"/>.
/// </summary>
public sealed class CurrentRoomIndexTests
{
    // -----------------------------------------------------------------------
    // StartRun — initial state
    // -----------------------------------------------------------------------

    [Fact]
    public void StartRun_ShouldInitializeCurrentRoomIndexToZero()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(), "seed-test", "gen-test", "markov-test",
            room, DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.CurrentRoomIndex.Should().Be(0);
    }

    [Fact]
    public void StartRun_ShouldCreateThresholdRoomAtCurrentRoomIndexZero()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(), "seed-threshold", "gen-test", "markov-test",
            room, DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.CurrentRoomIndex.Should().Be(0);
        run.CurrentRoom.RoomType.Should().Be(RoomType.Threshold);
    }

    [Fact]
    public void StartRun_ShouldExposeCurrentRoomIndexInRunDto()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(), "seed-dto", "gen-test", "markov-test",
            room, DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var dto = RunDto.FromDomain(run);

        dto.CurrentRoomIndex.Should().Be(0);
        dto.CurrentRoomNumber.Should().Be(1,
            because: "CurrentRoomNumber is one-based — first room is displayed as Salle 1.");
    }

    [Fact]
    public void RunDto_ShouldNotUseRoomDepthAsRoomCounter()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(), "seed-counter", "gen-test", "markov-test",
            room, DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var dto = RunDto.FromDomain(run);

        // Both are 0 at start but come from different sources:
        // CurrentDepth  = Room.Depth         (internal room-sequence ordinal, used by MoveToNextRoom)
        // CurrentRoomIndex = Run.CurrentRoomIndex (explicit infinite-run counter)
        // They must be distinct tracked properties, not aliases.
        dto.CurrentDepth.Should().Be(0, because: "Room.Depth of the Threshold room is 0.");
        dto.CurrentRoomIndex.Should().Be(0, because: "Run.CurrentRoomIndex is explicitly 0 at start.");

        // CurrentRoomNumber is 1-based for player display — it is not derived from Room.Depth.
        dto.CurrentRoomNumber.Should().Be(1,
            because: "CurrentRoomNumber == CurrentRoomIndex + 1, not Room.Depth + 1.");
        dto.CurrentRoomNumber.Should().NotBe(dto.CurrentRoom.Depth,
            because: "CurrentRoomNumber is 1-based while Room.Depth is 0-based; they differ in value.");
    }

    // -----------------------------------------------------------------------
    // CurrentRoomIndex must not change during intra-room progression
    // -----------------------------------------------------------------------

    [Fact]
    public void CurrentRoomIndex_ShouldRemainZero_WhileStillInFirstRoom()
    {
        var run = TestGameEngineFactory.CreateRun();
        var firstNode = run.CurrentRoom.AvailableNodes.First();

        TestGameEngineFactory.EnterNode(run, firstNode);

        run.CurrentRoomIndex.Should().Be(0,
            because: "Choosing a node within the current room must not change CurrentRoomIndex.");
    }

    [Fact]
    public void ResolveNode_ShouldNotChangeCurrentRoomIndex()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(
            NodeEventType.Combat);

        runWithNode.Run.ResolveCurrentEvent();

        runWithNode.Run.CurrentRoomIndex.Should().Be(0,
            because: "Resolving a node event does not move to a new room.");
    }

    [Fact]
    public void ProgressRunWithinRoom_ShouldNotChangeCurrentRoomIndex()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(
            NodeEventType.Combat);

        runWithNode.Run.ProgressCurrentRoom();

        runWithNode.Run.CurrentRoomIndex.Should().Be(0,
            because: "Progressing to the next node layer within a room does not move to a new room.");
    }

    [Fact]
    public void CompleteNonBossNode_ShouldNotChangeCurrentRoomIndex()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(
            NodeEventType.Combat);

        // Combat (row 0) is a non-boss node — resolving it must not touch CurrentRoomIndex.
        runWithNode.Run.CurrentRoomIndex.Should().Be(0,
            because: "Completing a non-boss node does not advance to the next room.");
        runWithNode.Run.Status.Should().Be(RunStatus.Active,
            because: "Run stays Active after a non-boss node resolution.");
    }

    [Fact]
    public void CompleteRoomBoss_ShouldNotIncrementCurrentRoomIndex()
    {
        // Resolve the boss node.
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        run.CurrentRoomIndex.Should().Be(0,
            because: "boss resolution does not increment CurrentRoomIndex — that is the " +
                     "responsibility of Run.ConfirmRoomExit.");
        run.Status.Should().Be(RunStatus.Active,
            because: "the boss no longer locks the run's status — see Run.ConfirmRoomExit.");
    }
}