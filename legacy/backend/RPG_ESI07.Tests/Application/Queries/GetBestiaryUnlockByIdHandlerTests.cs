using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.BestiaryUnlocks;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetBestiaryUnlockByIdHandlerTests
{
    private readonly Mock<IBestiaryUnlockRepository> _mockRepo;
    private readonly GetBestiaryUnlockByIdHandler _handler;

    public GetBestiaryUnlockByIdHandlerTests()
    {
        _mockRepo = new Mock<IBestiaryUnlockRepository>(MockBehavior.Strict);
        _handler = new GetBestiaryUnlockByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsOwner()
    {
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetBestiaryUnlockByIdQuery(1, 10, false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsAdmin()
    {
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetBestiaryUnlockByIdQuery(1, 99, true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenIdDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((BestiaryUnlock?)null);
        var query = new GetBestiaryUnlockByIdQuery(999, 1, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("BestiaryUnlock not found");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAdmin()
    {
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetBestiaryUnlockByIdQuery(1, 5, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Access forbidden");
    }
}