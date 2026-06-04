using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.UserConsents;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.UserConsents;

public class CreateUserConsentHandlerTests
{
    private readonly Mock<IUserConsentRepository> _mockRepo;
    private readonly CreateUserConsentHandler _handler;

    public CreateUserConsentHandlerTests()
    {
        _mockRepo = new Mock<IUserConsentRepository>();
        _handler = new CreateUserConsentHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<UserConsent>())).ReturnsAsync((UserConsent)null!);

        // Act
        var result = await _handler.Handle(new CreateUserConsentCommand(1, true, false), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("UserConsent created successfully");
    }
}

public class UpdateUserConsentHandlerTests
{
    private readonly Mock<IUserConsentRepository> _mockRepo;
    private readonly UpdateUserConsentHandler _handler;

    public UpdateUserConsentHandlerTests()
    {
        _mockRepo = new Mock<IUserConsentRepository>();
        _handler = new UpdateUserConsentHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerUpdates_ReturnsSuccess()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 1, AnalyticsConsent = false, MarketingConsent = false };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<UserConsent>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateUserConsentCommand(1, 1, true, true, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.AnalyticsConsent.Should().BeTrue();
        entity.MarketingConsent.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AdminUpdatesAnyUser_ReturnsSuccess()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 2, AnalyticsConsent = false, MarketingConsent = false };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<UserConsent>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new UpdateUserConsentCommand(1, 2, true, true, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherUserUpdates_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 2, AnalyticsConsent = false, MarketingConsent = false };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateUserConsentCommand(1, 2, true, true, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UserConsent?)null);

        // Act
        var act = async () => await _handler.Handle(
            new UpdateUserConsentCommand(99, 1, true, true, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class DeleteUserConsentHandlerTests
{
    private readonly Mock<IUserConsentRepository> _mockRepo;
    private readonly DeleteUserConsentHandler _handler;

    public DeleteUserConsentHandlerTests()
    {
        _mockRepo = new Mock<IUserConsentRepository>();
        _handler = new DeleteUserConsentHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_OwnerDeletes_ReturnsSuccess()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteUserConsentCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesAnyUser_ReturnsSuccess()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new DeleteUserConsentCommand(1, RequestingUserId: 99, IsAdmin: true),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherUserDeletes_ThrowsUnauthorized()
    {
        // Arrange
        var entity = new UserConsent { Id = 1, UserId = 2 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteUserConsentCommand(1, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ThrowsKeyNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UserConsent?)null);

        // Act
        var act = async () => await _handler.Handle(
            new DeleteUserConsentCommand(99, RequestingUserId: 1, IsAdmin: false),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}