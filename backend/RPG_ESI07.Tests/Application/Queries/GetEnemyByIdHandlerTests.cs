using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetEnemyByIdHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly GetEnemyByIdHandler _handler;

    public GetEnemyByIdHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>();
        _handler = new GetEnemyByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsEnemy()
    {
        var enemy = new Enemy { Id = 1, Name = "Goblin", Type = "basic", MaxHP = 50 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(enemy);

        var result = await _handler.Handle(new GetEnemyByIdQuery(1), CancellationToken.None);

        result.Enemy.Should().BeEquivalentTo(enemy);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Enemy?)null);

        var act = async () => await _handler.Handle(new GetEnemyByIdQuery(99), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectId()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Enemy?)null);

        try { await _handler.Handle(new GetEnemyByIdQuery(5), CancellationToken.None); }
        catch (KeyNotFoundException) { }

        _mockRepo.Verify(r => r.GetByIdAsync(5), Times.Once);
    }
}