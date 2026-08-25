using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Application.Runs.EnterGridNode;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Save / Abandon / Return-to-menu decisions. Saving requires a safe point
/// (<see cref="Run.IsAtSafePoint"/>); destructive abandonment remains available for recovery
/// from any open state. No dependency on the boss or the removed Interlude/RoomResolved states.
///
/// Group A: SaveAndExit domain behavior
/// Group B: AbandonRun destructive recovery
/// Group C: Guards — game actions blocked when Suspended or Abandoned
/// </summary>
public sealed class RunExitDecisionsTests
{
    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    /// <summary>A freshly started run: Active, room Active, nothing in progress — a safe
    /// point by construction, with no need to walk anywhere first.</summary>
    private static Run CreateRunAtSafePoint() => TestGameEngineFactory.CreateRun();

    /// <summary>Mid node-selection — the room has left <see cref="Domain.Rooms.RoomState.Active"/>
    /// but the event hasn't resolved yet. Not a safe point.</summary>
    private static Run CreateRunWithNodeSelected() =>
        TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Item).Run;

    /// <summary>Node resolved but the room hasn't returned to exploration yet
    /// (<see cref="ProgressRunCommand"/> not called). Still not a safe point.</summary>
    private static Run CreateRunWithNodeResolved() =>
        TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Item).Run;

    private static Run CreateRunSuspendedFromSafePoint()
    {
        var run = CreateRunAtSafePoint();
        run.SaveAndExit(DateTimeOffset.UtcNow);
        return run;
    }

    private static (SaveAndExitRunCommandHandler handler, Mock<IRunRepository> repo, Mock<IClock> clock)
        CreateSaveAndExitHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new SaveAndExitRunCommandHandler(
            repo.Object, Mock.Of<IPlayerProfileGateway>(), clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());
        return (handler, repo, clock);
    }

    private static (AbandonRunCommandHandler handler, Mock<IRunRepository> repo, Mock<IClock> clock)
        CreateAbandonHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new AbandonRunCommandHandler(repo.Object, Mock.Of<IOutboxWriter>(), clock.Object);
        return (handler, repo, clock);
    }

    // -----------------------------------------------------------------------
    // Group A — SaveAndExit domain behavior
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveAndExit_ShouldSucceed_WhenRunIsAtSafePoint()
    {
        var run = CreateRunAtSafePoint();
        run.IsAtSafePoint.Should().BeTrue();

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveAndExit_ShouldSetStatusToSuspended()
    {
        var run = CreateRunAtSafePoint();

        run.SaveAndExit(DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Suspended);
    }

    [Fact]
    public void SaveAndExit_ShouldSetSavedAt()
    {
        var run = CreateRunAtSafePoint();
        var savedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        run.SaveAndExit(savedAt);

        run.SavedAt.Should().Be(savedAt);
    }

    [Fact]
    public void SaveAndExit_ShouldMarkRunAsResumable()
    {
        var run = CreateRunAtSafePoint();

        run.SaveAndExit(DateTimeOffset.UtcNow);

        // CanResume is derived in RunDto; verify via Status directly
        run.Status.Should().Be(RunStatus.Suspended,
            because: "a suspended run is the one the client considers resumable.");
    }

    [Fact]
    public void SaveAndExit_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunAtSafePoint();
        var indexBefore = run.CurrentRoomIndex;

        run.SaveAndExit(DateTimeOffset.UtcNow);

        run.CurrentRoomIndex.Should().Be(indexBefore,
            because: "SaveAndExit must not advance the room index.");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRoomIsMidNodeSelection()
    {
        var run = CreateRunWithNodeSelected();
        run.IsAtSafePoint.Should().BeFalse();

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*event must be resolved*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRoomEventIsResolvedButNotProgressed()
    {
        var run = CreateRunWithNodeResolved();
        run.IsAtSafePoint.Should().BeFalse();

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*event must be resolved*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunHasPendingRewardOffer()
    {
        var run = CreateRunAtSafePoint();
        run.SetPendingRewardOffer(new RewardOfferId(Guid.NewGuid()));

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*pending reward offer*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunIsAlreadySuspended()
    {
        var run = CreateRunSuspendedFromSafePoint();
        run.Status.Should().Be(RunStatus.Suspended);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*suspended*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*closed*");
    }

    // -----------------------------------------------------------------------
    // Group B — AbandonRun destructive recovery (handler + domain)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AbandonRun_ShouldSucceed_WhenRunIsAtSafePoint()
    {
        var run = CreateRunAtSafePoint();
        run.IsAtSafePoint.Should().BeTrue();

        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Invoking(h => h.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None))
            .Should().NotThrowAsync();

        run.Status.Should().Be(RunStatus.Resolved);
        run.Outcome.Should().Be(RunOutcome.Abandon);
    }

    [Fact]
    public async Task AbandonRun_ShouldSetStatusToAbandoned()
    {
        var run = CreateRunAtSafePoint();
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.Status.Should().Be(RunStatus.Resolved);
        run.Outcome.Should().Be(RunOutcome.Abandon);
    }

    [Fact]
    public async Task AbandonRun_ShouldSetEndedAt()
    {
        var run = CreateRunAtSafePoint();
        var now = new DateTimeOffset(2026, 6, 1, 15, 30, 0, TimeSpan.Zero);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new AbandonRunCommandHandler(repo.Object, Mock.Of<IOutboxWriter>(), clock.Object);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.EndedAt.Should().Be(now);
    }

    [Fact]
    public async Task AbandonRun_ShouldNotBeResumable_AfterAbandoning()
    {
        var run = CreateRunAtSafePoint();
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.Status.Should().NotBe(RunStatus.Suspended,
            because: "an abandoned run is not suspended and cannot be resumed.");
    }

    [Fact]
    public async Task AbandonRun_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunAtSafePoint();
        var indexBefore = run.CurrentRoomIndex;
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.CurrentRoomIndex.Should().Be(indexBefore,
            because: "AbandonRun must not advance the room index.");
    }

    [Fact]
    public async Task AbandonRun_ShouldSucceed_WhenRoomIsMidNodeSelection()
    {
        var run = CreateRunWithNodeSelected();
        run.IsAtSafePoint.Should().BeFalse();

        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        run.Status.Should().Be(RunStatus.Resolved);
        run.Outcome.Should().Be(RunOutcome.Abandon);
    }

    [Fact]
    public async Task AbandonRun_ShouldFail_WhenRunIsAlreadyAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var (handler, _, _) = CreateAbandonHandler(run);

        var act = () => handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        // The domain "already closed" guard fires.
        await act.Should()
            .ThrowAsync<DomainException>();
    }

    // -----------------------------------------------------------------------
    // Group C — Guards: game actions blocked for Suspended / Abandoned runs
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldFail_WhenRunIsSuspended()
    {
        var run = CreateRunSuspendedFromSafePoint();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();
        var handler = new ProgressRunCommandHandler(repo.Object, choiceResolver.Object);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*suspended*");
    }

    [Fact]
    public async Task EnterGridNode_ShouldFail_WhenRunIsSuspended()
    {
        var run = CreateRunSuspendedFromSafePoint();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*suspended*");
    }

    [Fact]
    public async Task EnterGridNode_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task SaveAndExitHandler_ShouldPersistRun_WhenSuccessful()
    {
        var run = CreateRunAtSafePoint();
        var now = new DateTimeOffset(2026, 6, 9, 10, 0, 0, TimeSpan.Zero);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new SaveAndExitRunCommandHandler(
            repo.Object, Mock.Of<IPlayerProfileGateway>(), clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());
        var response = await handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Suspended.ToString());
        response.Run.CanResume.Should().BeTrue();
        response.Run.SavedAt.Should().Be(now);

        repo.Verify(
            r => r.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Suspended),
                CancellationToken.None),
            Times.Once);
    }
}
