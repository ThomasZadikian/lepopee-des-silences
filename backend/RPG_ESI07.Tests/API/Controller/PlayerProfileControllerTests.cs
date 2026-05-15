using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.PlayerProfiles;
using RPG_ESI07.Application.Queries.PlayerProfiles;
using RPG_ESI07.Domain;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class PlayerProfileControllerTests
{
    private readonly Mock<IMediator>         _mediatorMock;
    private readonly PlayerProfileController _controller;

    public PlayerProfileControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller   = new PlayerProfileController(_mediatorMock.Object);
    }

    private void SetUserContext(int userId, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, Constants.RoleAdmin));

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static GetPlayerProfileByUserIdResponse BuildProfileResponse(int userId = 1) => new(
        Id: 10, UserId: userId, CharacterName: "Aragorn",
        Level: 5, CurrentHP: 80, MaxHP: 100, CurrentMP: 30, MaxMP: 50,
        Strength: 15, Intelligence: 12, Speed: 11, Experience: 1250, Gold: 300,
        UpdatedAt: DateTime.UtcNow,
        TotalCombats: 20, CombatsWon: 15, CombatsLost: 5,
        TotalDamageDealt: 8000, TotalDamageTaken: 3000, TotalPlaytimeMinutes: 120,
        SavesCount: 2, InventoryCount: 1, SkillsCount: 3, BestiaryCount: 1
    );

    // ── GET /api/playerprofile/me ─────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_ReturnsOk_WithProfileData()
    {
        // Arrange
        SetUserContext(1);
        var expected = BuildProfileResponse(1);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetMyProfile_ExtractsUserIdFromJwt()
    {
        // Arrange
        SetUserContext(42);
        var expected = BuildProfileResponse(42);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == 42),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _controller.GetMyProfile();

        // Assert — le mediator reçoit bien l'userId 42 extrait du JWT
        _mediatorMock.Verify(
            m => m.Send(It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == 42),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── POST /api/playerprofile ───────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreated_WhenPlayerCreatesOwnProfile()
    {
        // Arrange
        SetUserContext(1);
        var command  = new CreatePlayerProfileCommand(UserId: 1, CharacterName: "Aragorn");
        var expected = new CreatePlayerProfileResponse(Id: 10, Message: "Personnage créé avec succès");

        _mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Create(command);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_ReturnsForbid_WhenPlayerCreatesForAnotherUser()
    {
        // Arrange
        SetUserContext(userId: 1, isAdmin: false);
        var command = new CreatePlayerProfileCommand(UserId: 99, CharacterName: "Hacker");

        // Act
        var result = await _controller.Create(command);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenAdminCreatesForAnyUser()
    {
        // Arrange
        SetUserContext(userId: 1, isAdmin: true);
        var command  = new CreatePlayerProfileCommand(UserId: 99, CharacterName: "NPC");
        var expected = new CreatePlayerProfileResponse(Id: 5, Message: "Personnage créé avec succès");

        _mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Create(command);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    // ── POST /api/playerprofile/sync ──────────────────────────────────────────

    [Fact]
    public async Task Sync_ReturnsOk_WhenSyncSucceeds()
    {
        // Arrange
        SetUserContext(1);
        var command = new SyncGameSaveCommand(
            RequestingUserId: 0, // sera écrasé par le JWT
            CurrentZone: "Forest", PositionX: 10f, PositionY: 20f, QuestFlags: null,
            Level: 5, CurrentHP: 80, MaxHP: 100, CurrentMP: 30, MaxMP: 50,
            Strength: 15, Intelligence: 12, Speed: 11, Experience: 1250, Gold: 300,
            TotalCombats: 20, CombatsWon: 15, CombatsLost: 5,
            TotalDamageDealt: 8000, TotalDamageTaken: 3000, TotalPlaytimeMinutes: 120
        );
        var expected = new SyncGameSaveResponse(true, "Sauvegarde synchronisée avec succès");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<SyncGameSaveCommand>(c => c.RequestingUserId == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Sync(command);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Sync_ForcesRequestingUserIdFromJwt()
    {
        // Arrange — le body envoie userId=99 mais le JWT dit userId=1
        SetUserContext(1);
        var command = new SyncGameSaveCommand(
            RequestingUserId: 99, // tentative de manipulation — doit être écrasé
            CurrentZone: "Forest", PositionX: 0f, PositionY: 0f, QuestFlags: null,
            Level: 1, CurrentHP: 100, MaxHP: 100, CurrentMP: 50, MaxMP: 50,
            Strength: 10, Intelligence: 10, Speed: 10, Experience: 0, Gold: 0,
            TotalCombats: 0, CombatsWon: 0, CombatsLost: 0,
            TotalDamageDealt: 0, TotalDamageTaken: 0, TotalPlaytimeMinutes: 0
        );
        var expected = new SyncGameSaveResponse(true, "OK");

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<SyncGameSaveCommand>(c => c.RequestingUserId == 1), // doit être 1, pas 99
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _controller.Sync(command);

        // Assert — le mediator reçoit bien RequestingUserId = 1 (depuis JWT)
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<SyncGameSaveCommand>(c => c.RequestingUserId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
