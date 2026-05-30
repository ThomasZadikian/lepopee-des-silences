using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class StartRunCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateRun_AndPersistIt()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = CreateInitialRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-001");
        generator.Setup(service => service.GenerateInitialRoom("seed-test-001")).Returns(initialRoom);

        var repository = new Mock<IRunRepository>();

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            clock.Object);

        var response = await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        response.Run.Id.Should().NotBeEmpty();
        response.Run.PlayerId.Should().Be(playerId);
        response.Run.Seed.Should().Be("seed-test-001");
        response.Run.GeneratorVersion.Should().Be("gen-0.1.0");
        response.Run.MarkovMatrixVersion.Should().Be("markov-0.1.0");
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentRoom.NodeLayers.SelectMany(layer => layer.Nodes).Should().HaveCount(4);

        repository.Verify(
            repo => repo.AddAsync(
                It.Is<Run>(run =>
                    run.PlayerId == playerId &&
                    run.Seed == "seed-test-001" &&
                    run.Status == RunStatus.Active),
                CancellationToken.None),
            Times.Once);
    }

    private static Room CreateInitialRoom()
    {
        return Room.Create(
            0,
            "Threshold",
            new[]
            {
                Node.Create(NodeEventType.Combat, 20, "common"),
                Node.Create(NodeEventType.Memory, 10, "common"),
                Node.Create(NodeEventType.Rest, 5, "none"),
                Node.Create(NodeEventType.Item, 15, "common")
            });
    }
}