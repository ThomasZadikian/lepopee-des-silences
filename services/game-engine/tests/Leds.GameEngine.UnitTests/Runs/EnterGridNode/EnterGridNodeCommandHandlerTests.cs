using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.EnterGridNode;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.EnterGridNode;

public sealed class EnterGridNodeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSelectTheNode_AndPersistTheRun()
    {
        var run = TestGameEngineFactory.CreateGridRun(NodeEventType.Item);
        var node = run.CurrentRoom.Nodes.First(n => !n.IsBoss);
        run.MoveParty(node.Lane, node.Row);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repo.Object);

        var response = await handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, node.Id.Value),
            CancellationToken.None);

        node.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
        response.Run.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new EnterGridNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new EnterGridNodeCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenPartyIsNotOnTheNodeCell()
    {
        var run = TestGameEngineFactory.CreateGridRun();
        var node = run.CurrentRoom.Nodes.First(n => !n.IsBoss);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, node.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
