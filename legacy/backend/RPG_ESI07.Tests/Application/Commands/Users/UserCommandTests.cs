using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Users;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Users;

public class CreateUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new CreateUserHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateUserCommand("testuser", "test@test.com", "hash123");
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = 1 });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("User created successfully");
    }
}

public class UpdateUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new UpdateUserHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesUsername()
    {
        // Arrange
        var entity = new User { Id = 1, Username = "old" };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new UpdateUserCommand(1, "newname", "new@test.com"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.Username.Should().Be("newname");
    }

    [Fact]
    public async Task Handle_NonExistingUser_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(new UpdateUserCommand(99, "x", "x"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }
}

public class DeleteUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly DeleteUserHandler _handler;

    public DeleteUserHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new DeleteUserHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_DeletesSuccessfully()
    {
        // Arrange
        var entity = new User { Id = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(entity), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(new DeleteUserCommand(99), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }
}