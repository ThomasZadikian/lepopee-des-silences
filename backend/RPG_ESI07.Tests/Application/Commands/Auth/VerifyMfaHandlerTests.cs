using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPG_ESI07.Tests.Application.Commands.Auth;

public class VerifyMfaHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IMfaService> _mockMfa;
    private readonly Mock<ITokenService> _mockToken;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly VerifyMfaHandler _handler;

    public VerifyMfaHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _mockMfa = new Mock<IMfaService>(MockBehavior.Strict);
        _mockToken = new Mock<ITokenService>(MockBehavior.Strict);
        _mockEncryption = new Mock<IEncryptionService>(MockBehavior.Strict);
        _handler = new VerifyMfaHandler(_mockRepo.Object, _mockMfa.Object, _mockToken.Object, _mockEncryption.Object);
    }

    [Fact]
    public async Task Handle_ReturnsJwt_WhenCodeIsValid()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = "Player", MfaEnabled = true, MfaSecret = secret };

        _mockToken.Setup(t => t.ValidateMfaToken("mfa-token")).Returns(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(secret)).Returns(secret);
        _mockMfa.Setup(m => m.ValidateCode(secret, "123456")).Returns(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockToken.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), "Player")).Returns("jwt-token");

        var result = await _handler.Handle(
            new VerifyMfaCommand(1, "mfa-token", "123456"),
            CancellationToken.None);

        result.success.Should().BeTrue();
        result.token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeIsInvalid()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = "Player", MfaEnabled = true, MfaSecret = secret };

        _mockToken.Setup(t => t.ValidateMfaToken("mfa-token")).Returns(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(secret)).Returns(secret);
        _mockMfa.Setup(m => m.ValidateCode(secret, "000000")).Returns(false);

        var result = await _handler.Handle(
            new VerifyMfaCommand(1, "mfa-token", "000000"),
            CancellationToken.None);

        result.success.Should().BeFalse();
        result.token.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenTokenIsInvalid()
    {
        _mockToken.Setup(t => t.ValidateMfaToken("invalid")).Returns((int?)null);

        var result = await _handler.Handle(
            new VerifyMfaCommand(1, "invalid", "123456"),
            CancellationToken.None);

        result.success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActivatesMfa_WhenNotYetEnabled()
    {
        var secret = new byte[] { 1, 2, 3 };
        var user = new User { Id = 1, Username = "testuser", Role = "Player", MfaEnabled = false, MfaSecret = secret };

        _mockToken.Setup(t => t.ValidateMfaToken("mfa-token")).Returns(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(secret)).Returns(secret);
        _mockMfa.Setup(m => m.ValidateCode(secret, "123456")).Returns(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockToken.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), "Player")).Returns("jwt-token");

        var result = await _handler.Handle(
            new VerifyMfaCommand(1, "mfa-token", "123456"),
            CancellationToken.None);

        result.success.Should().BeTrue();
        user.MfaEnabled.Should().BeTrue();
    }
}
