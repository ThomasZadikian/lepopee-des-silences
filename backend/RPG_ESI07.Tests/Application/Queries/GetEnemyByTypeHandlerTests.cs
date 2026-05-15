using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetEnemiesByTypeHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly GetEnemiesByTypeHandler _handler;

    public GetEnemiesByTypeHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>();
        _handler = new GetEnemiesByTypeHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidType_ReturnsFilteredEnemies()
    {
        var bosses = new List<Enemy>
        {
            new() { Id = 1, Name = "Dragon", Type = Constants.EnemyTypeBoss, MaxHP = 900 },
            new() { Id = 2, Name = "Liche",  Type = Constants.EnemyTypeBoss, MaxHP = 700 },
        };
        _mockRepo.Setup(r => r.GetByTypeAsync(Constants.EnemyTypeBoss)).ReturnsAsync(bosses);

        var result = await _handler.Handle(new GetEnemiesByTypeQuery(Constants.EnemyTypeBoss), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Type.Should().Be(Constants.EnemyTypeBoss));
    }

    [Fact]
    public async Task Handle_UnknownType_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetByTypeAsync("unknown")).ReturnsAsync(new List<Enemy>());

        var result = await _handler.Handle(new GetEnemiesByTypeQuery("unknown"), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectType()
    {
        _mockRepo.Setup(r => r.GetByTypeAsync(Constants.EnemyTypeMiniboss)).ReturnsAsync(new List<Enemy>());

        await _handler.Handle(new GetEnemiesByTypeQuery(Constants.EnemyTypeMiniboss), CancellationToken.None);

        _mockRepo.Verify(r => r.GetByTypeAsync(Constants.EnemyTypeMiniboss), Times.Once);
    }
}