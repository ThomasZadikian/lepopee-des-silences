using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Interlude;
using Leds.GameEngine.Application.Interlude.Dtos;
using Leds.GameEngine.Application.Interlude.EnterInterlude;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// PR 0.2.1 — Save / Abandon / Return-to-menu decisions.
///
/// Group A: SaveAndExit domain behavior (10 tests)
/// Group B: AbandonRun safe-point guard (8 tests)
/// Group C: Interlude exposes all 3 actions with correct metadata (7 tests)
/// Group D: Guards — game actions blocked when Suspended or Abandoned (6 tests)
/// </summary>
public sealed class RunExitDecisionsTests
{
    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

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

    private static (SaveAndExitRunCommandHandler handler, Mock<IRunRepository> repo, Mock<IClock> clock)
        CreateSaveAndExitHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new SaveAndExitRunCommandHandler(repo.Object, clock.Object);
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

        var handler = new AbandonRunCommandHandler(repo.Object, clock.Object);
        return (handler, repo, clock);
    }

    // -----------------------------------------------------------------------
    // Group A — SaveAndExit domain behavior
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveAndExit_ShouldSucceed_WhenRunIsRoomResolved()
    {
        var run = CreateRunInRoomResolved();
        run.Status.Should().Be(RunStatus.RoomResolved);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveAndExit_ShouldSucceed_WhenRunIsInterlude()
    {
        var run = CreateRunInInterlude();
        run.Status.Should().Be(RunStatus.Interlude);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveAndExit_ShouldSetStatusToSuspended()
    {
        var run = CreateRunInRoomResolved();

        run.SaveAndExit(DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Suspended);
    }

    [Fact]
    public void SaveAndExit_ShouldSetSavedAt()
    {
        var run = CreateRunInRoomResolved();
        var savedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        run.SaveAndExit(savedAt);

        run.SavedAt.Should().Be(savedAt);
    }

    [Fact]
    public void SaveAndExit_ShouldMarkRunAsResumable()
    {
        var run = CreateRunInRoomResolved();

        run.SaveAndExit(DateTimeOffset.UtcNow);

        // CanResume is derived in RunDto; verify via Status directly
        run.Status.Should().Be(RunStatus.Suspended,
            because: "a suspended run is the one the client considers resumable.");
    }

    [Fact]
    public void SaveAndExit_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunInRoomResolved();
        var indexBefore = run.CurrentRoomIndex;

        run.SaveAndExit(DateTimeOffset.UtcNow);

        run.CurrentRoomIndex.Should().Be(indexBefore,
            because: "SaveAndExit must not advance the room index.");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*safe point*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunHasPendingRewardOffer()
    {
        var run = CreateRunInRoomResolved();
        run.SetPendingRewardOffer(new RewardOfferId(Guid.NewGuid()));

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*pending reward offer*");
    }

    [Fact]
    public void SaveAndExit_ShouldFail_WhenRunIsAlreadySuspended()
    {
        var run = CreateRunSuspendedFromRoomResolved();
        run.Status.Should().Be(RunStatus.Suspended);

        var act = () => run.SaveAndExit(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*already suspended*");
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
    // Group B — AbandonRun safe-point guard (handler + domain)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AbandonRun_ShouldSucceed_WhenRunIsRoomResolved()
    {
        var run = CreateRunInRoomResolved();
        run.Status.Should().Be(RunStatus.RoomResolved);

        var (handler, repo, _) = CreateAbandonHandler(run);

        await handler.Invoking(h => h.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None))
            .Should().NotThrowAsync();

        run.Status.Should().Be(RunStatus.Abandoned);
    }

    [Fact]
    public async Task AbandonRun_ShouldSucceed_WhenRunIsInterlude()
    {
        var run = CreateRunInInterlude();
        run.Status.Should().Be(RunStatus.Interlude);

        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Invoking(h => h.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None))
            .Should().NotThrowAsync();

        run.Status.Should().Be(RunStatus.Abandoned);
    }

    [Fact]
    public async Task AbandonRun_ShouldSetStatusToAbandoned()
    {
        var run = CreateRunInRoomResolved();
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.Status.Should().Be(RunStatus.Abandoned);
    }

    [Fact]
    public async Task AbandonRun_ShouldSetEndedAt()
    {
        var run = CreateRunInRoomResolved();
        var now = new DateTimeOffset(2026, 6, 1, 15, 30, 0, TimeSpan.Zero);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new AbandonRunCommandHandler(repo.Object, clock.Object);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.EndedAt.Should().Be(now);
    }

    [Fact]
    public async Task AbandonRun_ShouldNotBeResumable_AfterAbandoning()
    {
        var run = CreateRunInRoomResolved();
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.Status.Should().NotBe(RunStatus.Suspended,
            because: "an abandoned run is not suspended and cannot be resumed.");
    }

    [Fact]
    public async Task AbandonRun_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunInRoomResolved();
        var indexBefore = run.CurrentRoomIndex;
        var (handler, _, _) = CreateAbandonHandler(run);

        await handler.Handle(new AbandonRunCommand(run.Id.Value), CancellationToken.None);

        run.CurrentRoomIndex.Should().Be(indexBefore,
            because: "AbandonRun must not advance the room index.");
    }

    [Fact]
    public async Task AbandonRun_ShouldFail_WhenRunIsActive()
    {
        // Handler-level guard: AbandonRun only allowed from safe points
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var (handler, _, _) = CreateAbandonHandler(run);

        var act = () => handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*safe point*");
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

        // Either the handler-level safe-point guard or the domain "already closed" guard fires
        await act.Should()
            .ThrowAsync<DomainException>();
    }

    // -----------------------------------------------------------------------
    // Group C — Interlude exposes all 3 actions with correct metadata
    // -----------------------------------------------------------------------

    [Fact]
    public void InterludeActions_ShouldExposeEnterNextRoom()
    {
        var actions = InterludeDto.BuildDefaultActions();

        actions.Should().ContainSingle(a => a.Key == "enter-next-room",
            because: "Interlude must expose the enter-next-room action.");
    }

    [Fact]
    public void InterludeActions_ShouldExposeSaveAndExit()
    {
        var actions = InterludeDto.BuildDefaultActions();

        actions.Should().ContainSingle(a => a.Key == "save-and-exit",
            because: "Interlude must expose the save-and-exit action.");
    }

    [Fact]
    public void InterludeActions_ShouldExposeAbandonRun()
    {
        var actions = InterludeDto.BuildDefaultActions();

        actions.Should().ContainSingle(a => a.Key == "abandon-run",
            because: "Interlude must expose the abandon-run action.");
    }

    [Fact]
    public void InterludeActions_AbandonRun_ShouldBeDangerous()
    {
        var actions = InterludeDto.BuildDefaultActions();
        var abandon = actions.Single(a => a.Key == "abandon-run");

        abandon.IsDangerous.Should().BeTrue(
            because: "abandoning a run is irreversible and must be marked dangerous.");
    }

    [Fact]
    public void InterludeActions_AbandonRun_ShouldRequireConfirmation()
    {
        var actions = InterludeDto.BuildDefaultActions();
        var abandon = actions.Single(a => a.Key == "abandon-run");

        abandon.RequiresConfirmation.Should().BeTrue(
            because: "irreversible actions must require explicit player confirmation.");
    }

    [Fact]
    public void InterludeActions_SaveAndExit_ShouldNotBeDangerous()
    {
        var actions = InterludeDto.BuildDefaultActions();
        var save = actions.Single(a => a.Key == "save-and-exit");

        save.IsDangerous.Should().BeFalse(
            because: "saving a run is non-destructive.");
    }

    [Fact]
    public void InterludeActions_SaveAndExit_ShouldNotRequireConfirmation()
    {
        var actions = InterludeDto.BuildDefaultActions();
        var save = actions.Single(a => a.Key == "save-and-exit");

        save.RequiresConfirmation.Should().BeFalse(
            because: "saving is reversible and should not gate on confirmation.");
    }

    // -----------------------------------------------------------------------
    // Group D — Guards: game actions blocked for Suspended / Abandoned runs
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldFail_WhenRunIsSuspended()
    {
        var run = CreateRunSuspendedFromRoomResolved();

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
    public async Task ChooseNode_ShouldFail_WhenRunIsSuspended()
    {
        var run = CreateRunSuspendedFromRoomResolved();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ChooseNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new ChooseNodeCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*suspended*");
    }

    [Fact]
    public async Task ChooseNode_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ChooseNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new ChooseNodeCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task EnterInterlude_ShouldFail_WhenRunIsSuspended()
    {
        var run = CreateRunSuspendedFromRoomResolved();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var provider = new DefaultInterludeNodeProvider();
        var handler = new EnterInterludeCommandHandler(repo.Object, provider);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task EnterInterlude_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var provider = new DefaultInterludeNodeProvider();
        var handler = new EnterInterludeCommandHandler(repo.Object, provider);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task SaveAndExitHandler_ShouldPersistRun_WhenSuccessful()
    {
        var run = CreateRunInRoomResolved();
        var now = new DateTimeOffset(2026, 6, 9, 10, 0, 0, TimeSpan.Zero);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new SaveAndExitRunCommandHandler(repo.Object, clock.Object);
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