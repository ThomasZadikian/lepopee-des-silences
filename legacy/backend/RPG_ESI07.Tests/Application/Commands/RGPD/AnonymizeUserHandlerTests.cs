using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.RGPD;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.RGPD;
public class AnonymizeUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly AnonymizeUserHandler _handler;

    public AnonymizeUserHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new AnonymizeUserHandler(_mockRepo.Object);
    }

    private static User BuildActiveUser(int id = 1) => new()
    {
        Id           = id,
        Username     = "testuser",
        Email        = new byte[] { 1, 2, 3 },
        PasswordHash = "argon2id$hash",
        MfaSecret    = new byte[] { 4, 5, 6 },
        MfaEnabled   = true,
        LastLoginIP  = "192.168.1.1",
        DeletedAt    = null,
    };


    [Fact]
    public async Task Handle_ActiveUser_ReturnsSuccess()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new AnonymizeUserCommand(1, "Demande personnelle"),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Art. 17");
    }

    [Fact]
    public async Task Handle_ActiveUser_AnonymizesUsername()
    {
        // Arrange
        var user = BuildActiveUser(5);
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(5, null), CancellationToken.None);

        // Assert — le username doit être remplacé par deleted_user_{id}
        user.Username.Should().Be("deleted_user_5");
    }

    [Fact]
    public async Task Handle_ActiveUser_ClearsPasswordHash()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — le hash doit être effacé (connexion impossible après)
        user.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ActiveUser_ClearsEmail()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — l'email chiffré doit être remplacé par un tableau vide
        user.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ActiveUser_DisablesMfa()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecret.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveUser_SetsDeletedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var user   = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — DeletedAt doit être défini et postérieur à before
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Handle_WithCustomReason_SetsReason()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            new AnonymizeUserCommand(1, "Je quitte le jeu"),
            CancellationToken.None);

        // Assert
        user.DeletionReason.Should().Be("Je quitte le jeu");
    }

    [Fact]
    public async Task Handle_WithNullReason_SetsDefaultReason()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — la raison par défaut doit mentionner le RGPD
        user.DeletionReason.Should().Contain("RGPD");
    }

    [Fact]
    public async Task Handle_ActiveUser_CallsUpdateAsync()
    {
        // Arrange
        var user = BuildActiveUser();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — UpdateAsync doit avoir été appelé exactement une fois
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }


    [Fact]
    public async Task Handle_AlreadyDeletedUser_ReturnsFalse()
    {
        // Arrange — compte déjà anonymisé
        var user = BuildActiveUser();
        user.DeletedAt = DateTime.UtcNow.AddDays(-5);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new AnonymizeUserCommand(1, null), CancellationToken.None);

        // Assert — on ne doit pas re-anonymiser, et UpdateAsync ne doit pas être appelé
        result.Success.Should().BeFalse();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(
            new AnonymizeUserCommand(99, null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
