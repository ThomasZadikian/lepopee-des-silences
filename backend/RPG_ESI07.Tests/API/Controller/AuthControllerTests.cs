using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.Auth;

namespace RPG_ESI07.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new AuthController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsOkResult_WhenRegistrationSucceeds()
    {
        // Arrange
        var command = new RegisterCommand("NouveauJoueur", "joueur@rpg.com", "MotDePasse123!");

        // success = true
        var expectedResponse = new AuthResponse(true, "fake-jwt-token", false, "Inscription réussie");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var command = new RegisterCommand("JoueurExistant", "existant@rpg.com", "MotDePasse123!");

        // success = false
        var failedResponse = new AuthResponse(false, null, false, "Cet email est déjà utilisé");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(failedResponse);

        // Act
        var result = await _controller.Register(command);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(failedResponse);
    }

    [Fact]
    public async Task Login_ReturnsOkResult_WhenLoginSucceeds()
    {
        // Arrange
        var command = new LoginCommand("MonUser", "MonPassword!");

        // success = true
        var expectedResponse = new AuthResponse(true, "valid-jwt-token", false, "Connexion réussie");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenLoginFails()
    {
        // Arrange
        var command = new LoginCommand("MonUser", "MauvaisPassword");

        // success = false
        var failedResponse = new AuthResponse(false, null, false, "Identifiants invalides");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(failedResponse);

        // Act
        var result = await _controller.Login(command);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(failedResponse);
    }
    [Fact]
    public async Task SetupMfa_ReturnsOk_WhenSuccessful()
    {
        var command = new SetupMfaCommand(1, "mfa-token");
        var expected = new SetupMfaResponse(true, "otpauth://totp/test", "ABCD1234", "Scannez le QR code.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.SetupMfa(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SetupMfa_ReturnsBadRequest_WhenFails()
    {
        var command = new SetupMfaCommand(1, "invalid-token");
        var expected = new SetupMfaResponse(false, null, null, "Token invalide.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.SetupMfa(command);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyMfa_ReturnsOk_WhenSuccessful()
    {
        var command = new VerifyMfaCommand(1, "mfa-token", "123456");
        var expected = new AuthResponse(true, "jwt-token", false, "Connexion réussie.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.VerifyMfa(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsUnauthorized_WhenFails()
    {
        var command = new VerifyMfaCommand(1, "mfa-token", "000000");
        var expected = new AuthResponse(false, null, false, "Code invalide.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.VerifyMfa(command);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task VerifyMfaLogin_ReturnsOk_WhenSuccessful()
    {
        var command = new LoginMfaCommand(1, "123456");
        var expected = new AuthResponse(true, "jwt-token", false, "Connexion réussie.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.VerifyMfaLogin(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task VerifyMfaLogin_ReturnsUnauthorized_WhenFails()
    {
        var command = new LoginMfaCommand(1, "000000");
        var expected = new AuthResponse(false, null, false, "Code invalide.");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

        var result = await _controller.VerifyMfaLogin(command);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}