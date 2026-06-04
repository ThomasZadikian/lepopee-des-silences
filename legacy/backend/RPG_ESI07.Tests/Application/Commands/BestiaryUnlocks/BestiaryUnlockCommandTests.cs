using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.BestiaryUnlocks;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.BestiaryUnlocks;

public class CreateBestiaryUnlockHandlerTests
{
    private readonly Mock<IBestiaryUnlockRepository> _mockRepo;
    private readonly CreateBestiaryUnlockHandler _handler;

    public CreateBestiaryUnlockHandlerTests()
    {
        _mockRepo = new Mock<IBestiaryUnlockRepository>();
        _handler = new CreateBestiaryUnlockHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_NewUnlock_CreatesAndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.GetByPlayerAndEnemyAsync(1, 2))
                 .ReturnsAsync((BestiaryUnlock?)null);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BestiaryUnlock>()))
                 .ReturnsAsync((BestiaryUnlock b) => b);

        var result = await _handler.Handle(
            new CreateBestiaryUnlockCommand(1, 2), CancellationToken.None);

        result.Message.Should().Be("BestiaryUnlock created successfully");
        _mockRepo.Verify(r => r.AddAsync(It.Is<BestiaryUnlock>(
            b => b.PlayerId == 1 && b.EnemyId == 2
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyUnlocked_ReturnsAlreadyUnlocked_DoesNotCallAdd()
    {
        var existing = new BestiaryUnlock { Id = 5, PlayerId = 1, EnemyId = 2 };
        _mockRepo.Setup(r => r.GetByPlayerAndEnemyAsync(1, 2))
                 .ReturnsAsync(existing);

        var result = await _handler.Handle(
            new CreateBestiaryUnlockCommand(1, 2), CancellationToken.None);

        result.Id.Should().Be(5);
        result.Message.Should().Be("Already unlocked.");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BestiaryUnlock>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BestiaryUnlock>())).ReturnsAsync((BestiaryUnlock)null!);

        // Act
        var result = await _handler.Handle(new CreateBestiaryUnlockCommand(1, 2), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("BestiaryUnlock created successfully");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BestiaryUnlock>()), Times.Once);
    }
}

public class UpdateBestiaryUnlockHandlerTests
{
    private readonly Mock<IBestiaryUnlockRepository> _mockRepo;
    private readonly UpdateBestiaryUnlockHandler _handler;

    public UpdateBestiaryUnlockHandlerTests()
    {
        _mockRepo = new Mock<IBestiaryUnlockRepository>();
        _handler = new UpdateBestiaryUnlockHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<BestiaryUnlock>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateBestiaryUnlockCommand(1, 1, 3, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.EnemyId.Should().Be(3);
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 2, EnemyId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<BestiaryUnlock>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateBestiaryUnlockCommand(1, 2, 3, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 2, EnemyId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateBestiaryUnlockCommand(1, 2, 3, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BestiaryUnlock?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateBestiaryUnlockCommand(99, 1, 1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeleteBestiaryUnlockHandlerTests
{
    private readonly Mock<IBestiaryUnlockRepository> _mockRepo;
    private readonly DeleteBestiaryUnlockHandler _handler;

    public DeleteBestiaryUnlockHandlerTests()
    {
        _mockRepo = new Mock<IBestiaryUnlockRepository>();
        _handler = new DeleteBestiaryUnlockHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteBestiaryUnlockCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteBestiaryUnlockCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new BestiaryUnlock { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteBestiaryUnlockCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BestiaryUnlock?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteBestiaryUnlockCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}