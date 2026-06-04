using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.PlayerSkills;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.PlayerSkills;

public class CreatePlayerSkillHandlerTests
{
    private readonly Mock<IPlayerSkillRepository> _mockRepo;
    private readonly CreatePlayerSkillHandler _handler;

    public CreatePlayerSkillHandlerTests()
    {
        _mockRepo = new Mock<IPlayerSkillRepository>();
        _handler = new CreatePlayerSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<PlayerSkill>())).ReturnsAsync((PlayerSkill)null!);

        // Act
        var result = await _handler.Handle(new CreatePlayerSkillCommand(1, 2, 3), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("PlayerSkill created successfully");
    }
}

public class UpdatePlayerSkillHandlerTests
{
    private readonly Mock<IPlayerSkillRepository> _mockRepo;
    private readonly UpdatePlayerSkillHandler _handler;

    public UpdatePlayerSkillHandlerTests()
    {
        _mockRepo = new Mock<IPlayerSkillRepository>();
        _handler = new UpdatePlayerSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 1, SkillId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerSkill>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdatePlayerSkillCommand(1, 1, 3, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.SkillId.Should().Be(3);
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 2, SkillId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerSkill>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdatePlayerSkillCommand(1, 2, 3, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 2, SkillId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdatePlayerSkillCommand(1, 2, 3, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PlayerSkill?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdatePlayerSkillCommand(99, 1, 1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeletePlayerSkillHandlerTests
{
    private readonly Mock<IPlayerSkillRepository> _mockRepo;
    private readonly DeletePlayerSkillHandler _handler;

    public DeletePlayerSkillHandlerTests()
    {
        _mockRepo = new Mock<IPlayerSkillRepository>();
        _handler = new DeletePlayerSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeletePlayerSkillCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyPlayer_ReturnsSuccess()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeletePlayerSkillCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherPlayerDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new PlayerSkill { Id = 1, PlayerId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeletePlayerSkillCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PlayerSkill?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeletePlayerSkillCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}