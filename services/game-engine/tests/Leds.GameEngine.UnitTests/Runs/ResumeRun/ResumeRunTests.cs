using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.ResumeRun;

public sealed class ResumeRunTests
{
    private static Run CreateRunInRoomResolved() =>
        TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

    private static Run CreateRunInInterlude()
    {
        var run = CreateRunInRoomResolved();
        run.EnterInterlude();
        return run;
    }

    private static Run CreateRunSuspendedFromRoomResolved()
    {
        var run = CreateRunInRoomResolved();
        run.SaveAndExit(DateTimeOffset.UtcNow);
        return run;
    }

    private static Run CreateRunSuspendedFromInterlude()
    {
        var run = CreateRunInInterlude();
        run.SaveAndExit(DateTimeOffset.UtcNow);
        return run;
    }

    private static Run CreateRunSuspendedFromActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        // Play through some nodes first
        var node = run.CurrentRoom.AvailableNodes.First();
        TestGameEngineFactory.EnterNode(run, node);
        run.ResolveCurrentEvent();
        run.ProgressCurrentRoom();
        // Now exit mid-room
        run.ExitMidRoom(DateTimeOffset.UtcNow);
        return run;
    }

    [Fact]
    public void Resume_ShouldRestoreActiveStatus_WhenPreSuspendWasActive()
    {
        var run = CreateRunSuspendedFromActive();
        run.Status.Should().Be(RunStatus.Suspended);

        run.Resume();

        run.Status.Should().Be(RunStatus.Active);
    }

    [Fact]
    public void Resume_ShouldRestoreRoomResolvedStatus_WhenPreSuspendWasRoomResolved()
    {
        var run = CreateRunSuspendedFromRoomResolved();

        run.Resume();

        run.Status.Should().Be(RunStatus.RoomResolved);
    }

    [Fact]
    public void Resume_ShouldRestoreInterludeStatus_WhenPreSuspendWasInterlude()
    {
        var run = CreateRunSuspendedFromInterlude();

        run.Resume();

        run.Status.Should().Be(RunStatus.Interlude);
    }

    [Fact]
    public void Resume_ShouldClearSavedAt()
    {
        var run = CreateRunSuspendedFromActive();
        run.SavedAt.Should().NotBeNull();

        run.Resume();

        run.SavedAt.Should().BeNull();
    }

    [Fact]
    public void Resume_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunSuspendedFromActive();
        var indexBefore = run.CurrentRoomIndex;

        run.Resume();

        run.CurrentRoomIndex.Should().Be(indexBefore);
    }

    [Fact]
    public void Resume_ShouldNotGenerateNewRoom()
    {
        var run = CreateRunSuspendedFromActive();
        var roomCountBefore = run.Rooms.Count;

        run.Resume();

        run.Rooms.Count.Should().Be(roomCountBefore);
    }

    [Fact]
    public void Resume_ShouldFail_WhenRunIsNotSuspended()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var act = () => run.Resume();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void Resume_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var act = () => run.Resume();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void Resume_ShouldFail_WhenRunIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.Resume();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void Resume_ShouldFail_WhenRunIsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.Resume();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void Resume_ShouldNotAllowSecondResume()
    {
        var run = CreateRunSuspendedFromActive();
        run.Resume();

        var act = () => run.Resume();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void Resume_ShouldAllowExitMidRoomAgain_WhenRestoredToActive()
    {
        var run = CreateRunSuspendedFromActive();

        run.Resume();

        run.ExitMidRoom(DateTimeOffset.UtcNow);
        run.Status.Should().Be(RunStatus.Suspended);
    }

    [Fact]
    public void Resume_ShouldAllowEnterNextRoom_WhenRestoredToInterlude()
    {
        var run = CreateRunSuspendedFromInterlude();

        run.Resume();

        run.Status.Should().Be(RunStatus.Interlude);

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        run.MoveToNextRoom(nextRoom);
        run.Status.Should().Be(RunStatus.Active);
    }

    [Fact]
    public void Resume_ShouldAllowEnterInterlude_WhenRestoredToRoomResolved()
    {
        var run = CreateRunSuspendedFromRoomResolved();

        run.Resume();

        run.Status.Should().Be(RunStatus.RoomResolved);

        run.EnterInterlude();
        run.Status.Should().Be(RunStatus.Interlude);
    }

    [Fact]
    public void Resume_ShouldNotIncrementCurrentRoomIndex_WhenResumedFromAnyStatus()
    {
        var runActive = CreateRunSuspendedFromActive();
        var runInterlude = CreateRunSuspendedFromInterlude();
        var runResolved = CreateRunSuspendedFromRoomResolved();

        var indexActiveBefore = runActive.CurrentRoomIndex;
        var indexInterludeBefore = runInterlude.CurrentRoomIndex;
        var indexResolvedBefore = runResolved.CurrentRoomIndex;

        runActive.Resume();
        runInterlude.Resume();
        runResolved.Resume();

        runActive.CurrentRoomIndex.Should().Be(indexActiveBefore);
        runInterlude.CurrentRoomIndex.Should().Be(indexInterludeBefore);
        runResolved.CurrentRoomIndex.Should().Be(indexResolvedBefore);
    }
}