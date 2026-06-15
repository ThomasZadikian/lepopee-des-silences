using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetRunById;

public sealed class GetRunByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRun_WhenRunExists()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new GetRunByIdQueryHandler(repository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.PlayerId.Should().Be(run.PlayerId);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        var allNodes = response.Run.CurrentRoom.Nodes.ToArray();

        allNodes.Should().HaveCount(response.Run.CurrentRoom.TotalNodeCount);
        allNodes.Should().HaveCount(6);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .HaveCount(2);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available" && node.Row == 0);

        allNodes
            .Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes
            .Should()
            .ContainSingle(node => node.IsBoss);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var handler = new GetRunByIdQueryHandler(repository.Object);

        var act = () => handler.Handle(
            new GetRunByIdQuery(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }
}