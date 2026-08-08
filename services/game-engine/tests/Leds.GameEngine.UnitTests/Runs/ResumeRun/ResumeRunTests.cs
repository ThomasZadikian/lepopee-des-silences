using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.ResumeRun;

public sealed class ResumeRunTests
{
    // SaveAndExit and ExitMidRoom both only ever suspend a run that is RunStatus.Active
    // (see Run.IsAtSafePoint / Run.ExitMidRoom) — RoomResolved and Interlude are no longer
    // produced, so Active is the only pre-suspend status left to cover.
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
}
