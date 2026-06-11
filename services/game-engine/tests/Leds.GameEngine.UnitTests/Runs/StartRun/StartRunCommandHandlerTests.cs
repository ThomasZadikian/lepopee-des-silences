using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class StartRunCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateRun_AndPersistIt()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-001");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-001", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    100, 0, 0,
                    ["skill.basic.strike", "skill.basic.guard"])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
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

        repository.Verify(
            repo => repo.AddAsync(
                It.Is<Run>(run =>
                    run.PlayerId == playerId &&
                    run.Seed == "seed-test-001" &&
                    run.Status == RunStatus.Active),
                CancellationToken.None),
            Times.Once);
    }

}