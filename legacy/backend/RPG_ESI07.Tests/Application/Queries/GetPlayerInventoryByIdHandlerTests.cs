using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.PlayerInventorys;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetPlayerInventoryByIdHandlerTests
{
    private readonly Mock<IPlayerInventoryRepository> _mockRepo;
    private readonly GetPlayerInventoryByIdHandler _handler;

    public GetPlayerInventoryByIdHandlerTests()
    {
        _mockRepo = new Mock<IPlayerInventoryRepository>(MockBehavior.Strict);
        _handler = new GetPlayerInventoryByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsOwner()
    {
        var entity = new PlayerInventory { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerInventoryByIdQuery(1, 10, false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsAdmin()
    {
        var entity = new PlayerInventory { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerInventoryByIdQuery(1, 99, true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenIdDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((PlayerInventory?)null);
        var query = new GetPlayerInventoryByIdQuery(999, 1, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Player inventory not found");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAdmin()
    {
        var entity = new PlayerInventory { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerInventoryByIdQuery(1, 5, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Access forbidden");
    }
}