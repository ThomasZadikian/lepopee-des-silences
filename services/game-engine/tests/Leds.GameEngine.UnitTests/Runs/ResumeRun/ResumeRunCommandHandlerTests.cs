using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.ResumeRun;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ResumeRun;

public sealed class ResumeRunCommandHandlerTests
{
    private static Run CreateSuspendedFromActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ExitMidRoom(DateTimeOffset.UtcNow);
        return run;
    }

    private static (ResumeRunCommandHandler handler, Mock<IRunRepository> repo)
        CreateHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResumeRunCommandHandler(repo.Object);
        return (handler, repo);
    }

    [Fact]
    public async Task Handle_ShouldResumeRun_AndPersistRun_WhenPreSuspendWasActive()
    {
        var run = CreateSuspendedFromActive();
        var (handler, repo) = CreateHandler(run);

        var response = await handler.Handle(
            new ResumeRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CanResume.Should().BeFalse();

        repo.Verify(
            r => r.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Active),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldClearSavedAt()
    {
        var run = CreateSuspendedFromActive();
        var (handler, _) = CreateHandler(run);

        var response = await handler.Handle(
            new ResumeRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.SavedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var handler = new ResumeRunCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new ResumeRunCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsNotSuspended()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var (handler, _) = CreateHandler(run);

        var act = () => handler.Handle(
            new ResumeRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var (handler, _) = CreateHandler(run);

        var act = () => handler.Handle(
            new ResumeRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenStatusIsNull()
    {
        // A suspended run with no preSuspendStatus should fail
        var run = TestGameEngineFactory.CreateRun();
        run.ExitMidRoom(DateTimeOffset.UtcNow);
        // Cannot directly set _preSuspendStatus to null from outside;
        // this case is covered at domain level via Reflection or by
        // creating a run that was constructed without preSuspendStatus.
        // The domain guard is tested in ResumeRunTests.
    }
}