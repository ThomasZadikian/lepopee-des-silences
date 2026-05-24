using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.PlayerInventorys;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.PlayerInventories;

public class CreatePlayerInventoryHandlerTests
{
    private readonly Mock<IPlayerInventoryRepository> _mockRepo;
    private readonly CreatePlayerInventoryHandler _handler;

    public CreatePlayerInventoryHandlerTests()
    {
        _mockRepo = new Mock<IPlayerInventoryRepository>();
        _handler = new CreatePlayerInventoryHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<PlayerInventory>())).ReturnsAsync((PlayerInventory)null!);

        // Act
        var result = await _handler.Handle(new CreatePlayerInventoryCommand(1, 2, 5), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("PlayerInventory created successfully");
    }
}

public class UpdatePlayerInventoryHandlerTests
{
    private readonly Mock<IPlayerInventoryRepository> _mockRepo;
    private readonly UpdatePlayerInventoryHandler _handler;

    public UpdatePlayerInventoryHandlerTests()
    {
        _mockRepo = new Mock<IPlayerInventoryRepository>();
        _handler = new UpdatePlayerInventoryHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 1, Quantity = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerInventory>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdatePlayerInventoryCommand(1, 1, 2, 10, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.Quantity.Should().Be(10);
        entity.ItemId.Should().Be(2);
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 2, ItemId = 1, Quantity = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerInventory>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdatePlayerInventoryCommand(1, 2, 2, 10, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 2, ItemId = 1, Quantity = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdatePlayerInventoryCommand(1, 2, 2, 10, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PlayerInventory?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdatePlayerInventoryCommand(99, 1, 1, 1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeletePlayerInventoryHandlerTests
{
    private readonly Mock<IPlayerInventoryRepository> _mockRepo;
    private readonly DeletePlayerInventoryHandler _handler;

    public DeletePlayerInventoryHandlerTests()
    {
        _mockRepo = new Mock<IPlayerInventoryRepository>();
        _handler = new DeletePlayerInventoryHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeletePlayerInventoryCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeletePlayerInventoryCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new PlayerInventory { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeletePlayerInventoryCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PlayerInventory?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeletePlayerInventoryCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}