using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Auth;

public class LoginMfaHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IMfaService> _mockMfa;
    private readonly Mock<ITokenService> _mockToken;
    private readonly LoginMfaHandler _handler;

    public LoginMfaHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _mockMfa = new Mock<IMfaService>(MockBehavior.Strict);
        _mockToken = new Mock<ITokenService>(MockBehavior.Strict);
        _handler = new LoginMfaHandler(_mockRepo.Object, _mockMfa.Object, _mockToken.Object);
    }

    [Fact]
    public async Task Handle_ReturnsJwt_WhenCodeIsValid()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = Constants.RolePlayer, MfaSecret = secret };

        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockMfa.Setup(m => m.ValidateCode(secret, "123456")).Returns(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockToken.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), Constants.RolePlayer)).Returns("jwt-token");

        var result = await _handler.Handle(new LoginMfaCommand(1, "123456"), CancellationToken.None);

        result.success.Should().BeTrue();
        result.token.Should().Be("jwt-token");
        result.userId.Should().Be(1);
        result.requiresMfa.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeIsInvalid()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = Constants.RolePlayer, MfaSecret = secret };

        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockMfa.Setup(m => m.ValidateCode(secret, "000000")).Returns(false);

        var result = await _handler.Handle(new LoginMfaCommand(1, "000000"), CancellationToken.None);

        result.success.Should().BeFalse();
        result.token.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenUserNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new LoginMfaCommand(999, "123456"), CancellationToken.None);

        result.success.Should().BeFalse();
        result.message.Should().Be("Identifiants incorrects.");
    }

    [Fact]
    public async Task Handle_Fails_WhenMfaSecretIsNull()
    {
        var user = new User { Id = 1, Username = "testuser", Role = Constants.RolePlayer, MfaSecret = null };

        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _handler.Handle(new LoginMfaCommand(1, "123456"), CancellationToken.None);

        result.success.Should().BeFalse();
        result.message.Should().Be("Identifiants incorrects.");
    }

    [Fact]
    public async Task Handle_UpdatesLastLoginAt_WhenSuccessful()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = Constants.RolePlayer, MfaSecret = secret };

        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockMfa.Setup(m => m.ValidateCode(secret, "123456")).Returns(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockToken.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), Constants.RolePlayer)).Returns("jwt-token");

        await _handler.Handle(new LoginMfaCommand(1, "123456"), CancellationToken.None);

        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }
}