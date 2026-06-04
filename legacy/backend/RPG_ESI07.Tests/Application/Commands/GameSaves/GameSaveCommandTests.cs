using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.GameSaves;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.GameSaves;

public class CreateGameSaveHandlerTests
{
    private readonly Mock<IGameSaveRepository> _mockRepo;
    private readonly CreateGameSaveHandler _handler;

    public CreateGameSaveHandlerTests()
    {
        _mockRepo = new Mock<IGameSaveRepository>();
        _handler = new CreateGameSaveHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<GameSave>())).ReturnsAsync((GameSave)null!);

        // Act
        var result = await _handler.Handle(new CreateGameSaveCommand(1, "Tutorial", 1), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("GameSave created successfully");
    }
}

public class UpdateGameSaveHandlerTests
{
    private readonly Mock<IGameSaveRepository> _mockRepo;
    private readonly UpdateGameSaveHandler _handler;

    public UpdateGameSaveHandlerTests()
    {
        _mockRepo = new Mock<IGameSaveRepository>();
        _handler = new UpdateGameSaveHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 1, CurrentZone = "Tutorial" };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateGameSaveCommand(1, 1, "BossFinal", 10, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.CurrentZone.Should().Be("BossFinal");
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 2, CurrentZone = "Tutorial" };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateGameSaveCommand(1, 2, "BossFinal", 10, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 2, CurrentZone = "Tutorial" };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateGameSaveCommand(1, 2, "BossFinal", 10, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((GameSave?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateGameSaveCommand(99, 1, "X", 1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeleteGameSaveHandlerTests
{
    private readonly Mock<IGameSaveRepository> _mockRepo;
    private readonly DeleteGameSaveHandler _handler;

    public DeleteGameSaveHandlerTests()
    {
        _mockRepo = new Mock<IGameSaveRepository>();
        _handler = new DeleteGameSaveHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteGameSaveCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteGameSaveCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new GameSave { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteGameSaveCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((GameSave?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteGameSaveCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}