using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Interlude;
using Leds.GameEngine.Application.Interlude.EnterInterlude;
using Leds.GameEngine.Domain.Interlude;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Interlude.EnterInterlude;

public sealed class EnterInterludeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldEnterInterlude_AndReturnResponse_WhenRoomIsResolved()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.Status.Should().Be(RunStatus.RoomResolved);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();
        nodeProvider.Setup(p => p.GetNodes(run)).Returns([
            InterludeNode.Create(InterludeNodeType.Player, "Player", "View your stats", "center", true, InterludeActionKey.ViewPlayer)
        ]);

        var handler = new EnterInterludeCommandHandler(runRepo.Object, nodeProvider.Object);

        var response = await handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        response.Interlude.Should().NotBeNull();
        response.Interlude!.Nodes.Should().NotBeEmpty();
        response.Interlude.AvailableActions.Should().NotBeEmpty();

        run.Status.Should().Be(RunStatus.Interlude);

        runRepo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new EnterInterludeCommandHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new EnterInterludeCommand(runId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsNotRoomResolved()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new EnterInterludeCommandHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*RoomResolved*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.Abandon(DateTimeOffset.UtcNow);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new EnterInterludeCommandHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*closed*");
    }
}
