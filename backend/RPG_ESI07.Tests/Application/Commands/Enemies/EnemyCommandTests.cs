using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Enemies;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands;

public class CreateEnemyHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly CreateEnemyHandler _handler;

    public CreateEnemyHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>(MockBehavior.Strict);
        _handler = new CreateEnemyHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_CreatesEnemy_AndReturnsResponse()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Enemy>())).Returns(Task.CompletedTask);

        var command = new CreateEnemyCommand(
            Name: "Rat Géant", Type: Constants.EnemyTypeBasic,
            MaxHP: 30, Strength: 6, Intelligence: 2, Speed: 14,
            PhysicalResistance: 1.0f, MagicalResistance: 1.0f,
            ExperienceReward: 10, GoldReward: 3,
            Description: "Un rat",
            InfluenceRadius: 5, TransitionMatrix: null,
            CombatScripts: null, MapStates: null,
            InitialState: Constants.EnemyStateRepos);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        _mockRepo.Verify(r => r.AddAsync(It.Is<Enemy>(e =>
            e.Name == "Rat Géant" &&
            e.Type == Constants.EnemyTypeBasic &&
            e.MaxHP == 30
        )), Times.Once);
    }
}

public class UpdateEnemyHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly UpdateEnemyHandler _handler;

    public UpdateEnemyHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>(MockBehavior.Strict);
        _handler = new UpdateEnemyHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_UpdatesEnemy_AndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Enemy>())).Returns(Task.CompletedTask);

        var command = new UpdateEnemyCommand(
            Id: 1,
            Name: "Rat Géant Modifié", Type: Constants.EnemyTypeBasic,
            MaxHP: 50, Strength: 8, Intelligence: 2, Speed: 14,
            PhysicalResistance: 1.0f, MagicalResistance: 1.0f,
            ExperienceReward: 15, GoldReward: 5,
            Description: "Un rat plus fort",
            InfluenceRadius: 5, TransitionMatrix: null,
            CombatScripts: null, MapStates: null,
            InitialState: Constants.EnemyStateRepos);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.UpdateAsync(It.Is<Enemy>(e =>
            e.Id == 1 && e.MaxHP == 50
        )), Times.Once);
    }
}

public class DeleteEnemyHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly DeleteEnemyHandler _handler;

    public DeleteEnemyHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>(MockBehavior.Strict);
        _handler = new DeleteEnemyHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_DeletesEnemy_AndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new DeleteEnemyCommand(1), CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}

