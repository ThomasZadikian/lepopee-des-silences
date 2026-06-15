using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.MoveToNextRoom;

public sealed class MoveToNextRoomCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMoveRunToNextRoom_WhenCurrentRoomIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator
            .Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None))
            .ReturnsAsync(nextRoom);

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object,
            generator.Object);

        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentDepth.Should().Be(nextRoom.Depth);
        response.Run.CurrentRoom.Id.Should().Be(nextRoom.Id.Value);

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
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

        var generator = new Mock<IRunGenerator>();

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object,
            generator.Object);

        var act = () => handler.Handle(
            new MoveToNextRoomCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }
}