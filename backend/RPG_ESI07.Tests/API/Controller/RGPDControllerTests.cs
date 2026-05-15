using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.RGPD;
using RPG_ESI07.Application.Queries.RGPD;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class RGPDControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly RgpdController _controller;

    public RGPDControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new RgpdController(_mediatorMock.Object);
    }

    // ── Helper : simule un utilisateur connecté avec son userId dans le JWT ──
    private void SetUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static GetUserDataResponse BuildResponse(int userId) => new(
        UserId:          userId,
        Username:        "testuser",
        CreatedAt:       new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        LastLoginAt:     new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        LastLoginIP:     Constants.SeedIpAddress,
        GameSaves:       new List<GameSave>(),
        Inventory:       new List<PlayerInventory>(),
        Skills:          new List<PlayerSkill>(),
        BestiaryUnlocks: new List<BestiaryUnlock>(),
        CombatStats:     null
    );

    // ── GET /api/rgpd/export — Article 15 ────────────────────────────────────

    [Fact]
    public async Task ExportMyData_ReturnsOk_WithUserData()
    {
        // Arrange
        SetUser(1);
        var response = BuildResponse(1);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ExportMyData();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task ExportMyData_SendsQueryWithCorrectUserId()
    {
        // Arrange
        SetUser(42);
        var response = BuildResponse(42);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 42), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _controller.ExportMyData();

        // Assert — le mediator doit avoir reçu l'userId 42 extrait du JWT
        _mediatorMock.Verify(
            m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 42), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DELETE /api/rgpd/me — Article 17 ─────────────────────────────────────

    [Fact]
    public async Task DeleteMyAccount_ReturnsOk_WhenSuccess()
    {
        // Arrange
        SetUser(1);
        var response = new AnonymizeUserResponse(true, "Compte anonymise avec succes conformement a l'Art. 17 du RGPD");
        _mediatorMock
            .Setup(m => m.Send(It.Is<AnonymizeUserCommand>(c => c.UserId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.DeleteMyAccount("Je quitte le jeu");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().Be(response.Message);
    }

    [Fact]
    public async Task DeleteMyAccount_ReturnsBadRequest_WhenAlreadyDeleted()
    {
        // Arrange
        SetUser(1);
        var response = new AnonymizeUserResponse(false, "Compte deja supprime");
        _mediatorMock
            .Setup(m => m.Send(It.Is<AnonymizeUserCommand>(c => c.UserId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.DeleteMyAccount(null);

        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
        bad.Value.Should().Be("Compte deja supprime");
    }

    [Fact]
    public async Task DeleteMyAccount_PassesReasonToCommand()
    {
        // Arrange
        SetUser(1);
        var response = new AnonymizeUserResponse(true, "OK");
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<AnonymizeUserCommand>(c => c.UserId == 1 && c.Reason == "Ma raison"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.DeleteMyAccount("Ma raison");

        // Assert — la raison doit être transmise telle quelle à la command
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<AnonymizeUserCommand>(c => c.Reason == "Ma raison"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteMyAccount_WithNullReason_StillWorks()
    {
        // Arrange
        SetUser(1);
        var response = new AnonymizeUserResponse(true, "OK");
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<AnonymizeUserCommand>(c => c.UserId == 1 && c.Reason == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.DeleteMyAccount(null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GET /api/rgpd/export/json — Article 20 ───────────────────────────────

    [Fact]
    public async Task ExportMyDataAsJson_ReturnsFileResult()
    {
        // Arrange
        SetUser(1);
        var response = BuildResponse(1);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ExportMyDataAsJson();

        // Assert — doit retourner un FileContentResult (téléchargement)
        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task ExportMyDataAsJson_FileNameContainsUserId()
    {
        // Arrange
        SetUser(7);
        var response = BuildResponse(7);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 7), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ExportMyDataAsJson();

        // Assert — le nom du fichier doit contenir l'userId
        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Contain("7");
    }

    [Fact]
    public async Task ExportMyDataAsJson_FileContentIsValidJson()
    {
        // Arrange
        SetUser(1);
        var response = BuildResponse(1);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetUserDataQuery>(q => q.UserId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ExportMyDataAsJson();

        // Assert — le contenu doit être du JSON valide et parseable
        var file = result.Should().BeOfType<FileContentResult>().Subject;
        var json = System.Text.Encoding.UTF8.GetString(file.FileContents);
        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
