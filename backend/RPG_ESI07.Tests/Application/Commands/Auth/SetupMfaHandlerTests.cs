using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Auth;

public class SetupMfaHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IMfaService> _mockMfa;
    private readonly Mock<ITokenService> _mockToken;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly SetupMfaHandler _handler;

    public SetupMfaHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _mockMfa = new Mock<IMfaService>(MockBehavior.Strict);
        _mockToken = new Mock<ITokenService>(MockBehavior.Strict);
        _mockEncryption = new Mock<IEncryptionService>(MockBehavior.Strict);
        _handler = new SetupMfaHandler(_mockRepo.Object, _mockMfa.Object, _mockToken.Object, _mockEncryption.Object);
    }

    [Fact]
    public async Task Handle_ReturnsQrCode_WhenTokenIsValid()
    {
        var secret = new byte[] { 1, 2, 3, 4, 5 };
        var user = new User { Id = 1, Username = "testuser" };

        _mockToken.Setup(t => t.ValidateMfaToken("valid-token")).Returns(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockMfa.Setup(m => m.GenerateSecret()).Returns(secret);
        _mockMfa.Setup(m => m.SecretToBase32(secret)).Returns("BASE32SECRET");
        _mockMfa.Setup(m => m.GetQrCodeUri("BASE32SECRET", "testuser")).Returns("otpauth://totp/...");
        _mockEncryption.Setup(e => e.Encrypt(secret)).Returns(new byte[] { 10, 11, 12, 13, 14 });
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new SetupMfaCommand(1, "valid-token"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.QrCodeUri.Should().Be("otpauth://totp/...");
        result.Secret.Should().Be("BASE32SECRET");
    }

    [Fact]
    public async Task Handle_Fails_WhenTokenIsInvalid()
    {
        _mockToken.Setup(t => t.ValidateMfaToken("invalid-token")).Returns((int?)null);

        var result = await _handler.Handle(
            new SetupMfaCommand(1, "invalid-token"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.QrCodeUri.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenUserNotFound()
    {
        _mockToken.Setup(t => t.ValidateMfaToken("valid-token")).Returns(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

        var result = await _handler.Handle(
            new SetupMfaCommand(1, "valid-token"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Fails_WhenUserIdMismatch()
    {
        _mockToken.Setup(t => t.ValidateMfaToken("valid-token")).Returns(2);

        var result = await _handler.Handle(
            new SetupMfaCommand(1, "valid-token"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}