using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllEnemiesHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly GetAllEnemiesHandler _handler;

    public GetAllEnemiesHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>();
        _handler = new GetAllEnemiesHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllEnemies()
    {
        var enemies = new List<Enemy>
        {
            new() { Id = 1, Name = "Goblin",  Type = Constants.EnemyTypeBasic,    MaxHP = 50 },
            new() { Id = 2, Name = "Dragon",  Type = Constants.EnemyTypeBoss,     MaxHP = 900 },
            new() { Id = 3, Name = "Golem",   Type = Constants.EnemyTypeMiniboss, MaxHP = 250 },
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(enemies);

        var result = await _handler.Handle(new GetAllEnemiesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items.Should().Contain(e => e.Name == "Goblin");
        result.Items.Should().Contain(e => e.Name == "Dragon");
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Enemy>());

        var result = await _handler.Handle(new GetAllEnemiesQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryOnce()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Enemy>());

        await _handler.Handle(new GetAllEnemiesQuery(), CancellationToken.None);

        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}