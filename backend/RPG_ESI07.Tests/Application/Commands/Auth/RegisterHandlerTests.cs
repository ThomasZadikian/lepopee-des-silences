using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Auth;

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly Mock<ITokenService> _mockToken;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockToken = new Mock<ITokenService>();
        _handler = new RegisterHandler(
        _mockRepo.Object,
        _mockHasher.Object,
        _mockToken.Object);
    }

    [Fact]
    public async Task Handle_NewUser_ReturnsToken()
    {
        // Arrange
        _mockRepo.Setup(r =>
        r.UsernameExistsAsync("newuser"))
        .ReturnsAsync(false);
        _mockHasher.Setup(h =>
        h.HashPassword("Pass123!"))
        .Returns("hashed");
        _mockRepo.Setup(r =>
        r.AddAsync(It.IsAny<User>()))
        .ReturnsAsync(new User { Id = 1 });
        _mockToken.Setup(t =>
        t.GenerateAccessToken(
        It.IsAny<User>(), Constants.RolePlayer))
        .Returns("jwt-token");
        var cmd = new RegisterCommand(
"newuser", "test@test.com", "Pass123!");
        // Act
        var result = await _handler.Handle(
        cmd, CancellationToken.None);
        // Assert
        result.success.Should().BeTrue();
        result.token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_DuplicateUser_Fails()
    {
        // Arrange
        _mockRepo.Setup(r =>
        r.UsernameExistsAsync("existing"))
        .ReturnsAsync(true);
        // Act
        var result = await _handler.Handle(
        new RegisterCommand(
        "existing", "x@x.com", "Pass123!"),
        CancellationToken.None);
        // Assert
        result.success.Should().BeFalse();
        result.token.Should().BeNull();
    }
}