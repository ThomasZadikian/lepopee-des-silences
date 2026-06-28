using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Interlude;
using Leds.GameEngine.Application.Interlude.GetInterlude;
using Leds.GameEngine.Domain.Interlude;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Interlude.GetInterlude;

public sealed class GetInterludeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnInterlude_WhenRunIsInInterludeState()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();
        nodeProvider.Setup(p => p.GetNodes(run)).Returns([
            InterludeNode.Create(InterludeNodeType.Player, "Player", "View your stats", "center", true, InterludeActionKey.ViewPlayer),
            InterludeNode.Create(InterludeNodeType.Inventory, "Inventory", "View your items", "left", true, InterludeActionKey.OpenInventory)
        ]);

        var handler = new GetInterludeQueryHandler(runRepo.Object, nodeProvider.Object);

        var response = await handler.Handle(
            new GetInterludeQuery(run.Id.Value),
            CancellationToken.None);

        response.Interlude.Should().NotBeNull();
        response.Interlude!.Nodes.Should().HaveCount(2);
        response.Interlude.AvailableActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new GetInterludeQueryHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new GetInterludeQuery(runId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsNotInInterlude()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new GetInterludeQueryHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new GetInterludeQuery(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*not in Interlude*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.CompleteRun(DateTimeOffset.UtcNow);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var nodeProvider = new Mock<IInterludeNodeProvider>();

        var handler = new GetInterludeQueryHandler(runRepo.Object, nodeProvider.Object);

        var act = () => handler.Handle(
            new GetInterludeQuery(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*not in Interlude*");
    }
}
