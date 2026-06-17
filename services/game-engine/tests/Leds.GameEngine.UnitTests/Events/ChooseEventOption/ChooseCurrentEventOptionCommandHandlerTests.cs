using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChooseEventOption;

public sealed class ChooseCurrentEventOptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldResolveChoice_WhenCurrentEventIsResolved()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(runWithNode.Run.Id, CancellationToken.None))
            .ReturnsAsync(runWithNode.Run);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();
        dispatcher
            .Setup(service => service.ResolveAsync(It.IsAny<CurrentEventChoiceResolutionContext>(), CancellationToken.None))
            .ReturnsAsync(CurrentEventChoiceResolutionResult.Create(
                "listen",
                accepted: true,
                "Choice resolved.",
                new[]
                {
                    new NarrativeFragmentDto("Elise", "Test fragment.")
                }));

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var response = await handler.Handle(
            new ChooseCurrentEventOptionCommand(runWithNode.Run.Id.Value, "listen"),
            CancellationToken.None);

        response.Run.Id.Should().Be(runWithNode.Run.Id.Value);
        response.Result.ChoiceId.Should().Be("listen");
        response.Result.Accepted.Should().BeTrue();
        response.Result.Message.Should().Be("Choice resolved.");
        response.Result.NarrativeFragments.Should().ContainSingle();
        runWithNode.TargetNode.ChosenEventOptionId.Should().Be("listen");
        runWithNode.TargetNode.HasChosenEventOption.Should().BeTrue();

        dispatcher.Verify(
            service => service.ResolveAsync(
                It.Is<CurrentEventChoiceResolutionContext>(context =>
                    context.Run == runWithNode.Run &&
                    context.Node.Id == runWithNode.TargetNode.Id &&
                    context.ChoiceId == "listen"),
                CancellationToken.None),
            Times.Once);

        repository.Verify(
            repo => repo.UpdateAsync(runWithNode.Run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = async () => await handler.Handle(
            new ChooseCurrentEventOptionCommand(runId, "listen"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        dispatcher.Verify(
            service => service.ResolveAsync(It.IsAny<CurrentEventChoiceResolutionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<Run>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCurrentEventIsNotResolved()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Npc);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(runWithNode.Run.Id, CancellationToken.None))
            .ReturnsAsync(runWithNode.Run);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = async () => await handler.Handle(
            new ChooseCurrentEventOptionCommand(runWithNode.Run.Id.Value, "listen"),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Current event must be resolved before choosing an event option.");

        dispatcher.Verify(
            service => service.ResolveAsync(It.IsAny<CurrentEventChoiceResolutionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenChoiceWasAlreadyResolved()
    {
        // Arrange
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);

        runWithNode.TargetNode.ChooseEventOption("listen");

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(runWithNode.Run.Id, CancellationToken.None))
            .ReturnsAsync(runWithNode.Run);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();
        dispatcher
            .Setup(service => service.ResolveAsync(It.IsAny<CurrentEventChoiceResolutionContext>(), CancellationToken.None))
            .ReturnsAsync(CurrentEventChoiceResolutionResult.Create(
                "leave",
                accepted: true,
                "Choice resolved."));

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        // Act
        var act = async () => await handler.Handle(
            new ChooseCurrentEventOptionCommand(runWithNode.Run.Id.Value, "leave"),
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Current event choice has already been resolved.");

        repository.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Run>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
