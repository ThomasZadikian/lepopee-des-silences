using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.CombatStatss;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.CombatStatss;

public class CreateCombatStatsHandlerTests
{
    private readonly Mock<ICombatStatsRepository> _mockRepo;
    private readonly CreateCombatStatsHandler _handler;

    public CreateCombatStatsHandlerTests()
    {
        _mockRepo = new Mock<ICombatStatsRepository>();
        _handler = new CreateCombatStatsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<RPG_ESI07.Domain.Entities.CombatStats>())).ReturnsAsync((RPG_ESI07.Domain.Entities.CombatStats)null!);

        // Act
        var result = await _handler.Handle(new CreateCombatStatsCommand(1), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("CombatStats created successfully");
    }
}

public class UpdateCombatStatsHandlerTests
{
    private readonly Mock<ICombatStatsRepository> _mockRepo;
    private readonly UpdateCombatStatsHandler _handler;

    public UpdateCombatStatsHandlerTests()
    {
        _mockRepo = new Mock<ICombatStatsRepository>();
        _handler = new UpdateCombatStatsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RPG_ESI07.Domain.Entities.CombatStats>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateCombatStatsCommand(1, 1, 10, 8, 2, 5000, 2000, 120, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.TotalCombats.Should().Be(10);
        entity.CombatsWon.Should().Be(8);
        entity.CombatsLost.Should().Be(2);
        entity.TotalDamageDealt.Should().Be(5000);
        entity.TotalDamageTaken.Should().Be(2000);
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RPG_ESI07.Domain.Entities.CombatStats>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateCombatStatsCommand(1, 2, 10, 8, 2, 5000, 2000, 120, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateCombatStatsCommand(1, 2, 10, 8, 2, 5000, 2000, 120, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RPG_ESI07.Domain.Entities.CombatStats?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateCombatStatsCommand(99, 1, 0, 0, 0, 0, 0, 0, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeleteCombatStatsHandlerTests
{
    private readonly Mock<ICombatStatsRepository> _mockRepo;
    private readonly DeleteCombatStatsHandler _handler;

    public DeleteCombatStatsHandlerTests()
    {
        _mockRepo = new Mock<ICombatStatsRepository>();
        _handler = new DeleteCombatStatsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteCombatStatsCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteCombatStatsCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new RPG_ESI07.Domain.Entities.CombatStats { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteCombatStatsCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RPG_ESI07.Domain.Entities.CombatStats?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteCombatStatsCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}