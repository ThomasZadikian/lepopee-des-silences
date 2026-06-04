using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.CombatStatss;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetCombatStatsByIdHandlerTests
{
    private readonly Mock<ICombatStatsRepository> _mockRepo;
    private readonly GetCombatStatsByIdHandler _handler;

    public GetCombatStatsByIdHandlerTests()
    {
        _mockRepo = new Mock<ICombatStatsRepository>(MockBehavior.Strict);
        _handler = new GetCombatStatsByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsOwner()
    {
        var entity = new CombatStats { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetCombatStatsByIdQuery(1, 10, false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsAdmin()
    {
        var entity = new CombatStats { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetCombatStatsByIdQuery(1, 99, true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenIdDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CombatStats?)null);
        var query = new GetCombatStatsByIdQuery(999, 1, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("CombatStats not found");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAdmin()
    {
        var entity = new CombatStats { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetCombatStatsByIdQuery(1, 5, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Access forbidden");
    }
}