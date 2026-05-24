using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.BestiaryUnlocks;
using RPG_ESI07.Application.Queries.BestiaryUnlocks;
using RPG_ESI07.Application.Queries.PlayerProfiles;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class BestiaryUnlocksControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly BestiaryUnlocksController _controller;

    public BestiaryUnlocksControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new BestiaryUnlocksController(_mediatorMock.Object);
    }

    private void SetUserContext(int userId, bool isAdmin = false)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, Constants.RoleAdmin));

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private GetPlayerProfileByUserIdResponse BuildProfile(int userId, int playerId)
    {
        return new GetPlayerProfileByUserIdResponse(
            playerId, userId, "Hero", 1, 100, 100, 50, 50, 10, 10, 10, 0, 0,
            DateTime.UtcNow, null, null, null, null, null, null, 0, 0, 0, 0);
    }

    private void SetupProfileLookup(int userId, int playerId)
    {
        var profile = BuildProfile(userId, playerId);
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithExpectedData()
    {
        var items = new List<BestiaryUnlock> { new BestiaryUnlock { Id = 1 } };
        var expectedResponse = new GetAllBestiaryUnlocksResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllBestiaryUnlocksQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_UsesPlayerIdFromProfile()
    {
        int targetId = 5;
        int currentUserId = 10;
        int playerId = 20;
        SetUserContext(currentUserId, isAdmin: true);
        SetupProfileLookup(currentUserId, playerId);

        var expectedUnlock = new BestiaryUnlock { Id = targetId };
        var expectedResponse = new GetBestiaryUnlockByIdResponse(expectedUnlock);

        _mediatorMock.Setup(m => m.Send(It.Is<GetBestiaryUnlockByIdQuery>(q =>
                        q.Id == targetId &&
                        q.RequestingUserId == playerId &&
                        q.IsAdmin == true), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.GetById(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenProfileNotFound()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var result = await _controller.GetById(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsForbid_WhenNonAdminCreatesForAnotherPlayer()
    {
        int currentUserId = 10;
        int playerId = 20;
        int targetPlayerId = 15;
        SetUserContext(currentUserId, isAdmin: false);
        SetupProfileLookup(currentUserId, playerId);

        var command = new CreateBestiaryUnlockCommand(targetPlayerId, 1);

        var result = await _controller.Create(command);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenNonAdminCreatesForSelf()
    {
        int currentUserId = 10;
        int playerId = 15;
        SetUserContext(currentUserId, isAdmin: false);
        SetupProfileLookup(currentUserId, playerId);

        var command = new CreateBestiaryUnlockCommand(playerId, 1);
        var expectedResponse = new CreateBestiaryUnlockResponse(5, "Created");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Create(command);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expectedResponse.Id);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenProfileNotFoundForNonAdmin()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var command = new CreateBestiaryUnlockCommand(15, 1);

        var result = await _controller.Create(command);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_Admin_SkipsProfileCheck()
    {
        int currentUserId = 10;
        int targetPlayerId = 99;
        SetUserContext(currentUserId, isAdmin: true);

        var command = new CreateBestiaryUnlockCommand(targetPlayerId, 1);
        var expectedResponse = new CreateBestiaryUnlockResponse(5, "Created");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Create(command);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdMismatch()
    {
        var command = new UpdateBestiaryUnlockCommand(2, 10, 1, 0, false);

        var result = await _controller.Update(1, command);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_ReturnsOkResult_AppliesPlayerIdToCommand()
    {
        int currentUserId = 10;
        int playerId = 20;
        SetUserContext(currentUserId, isAdmin: true);
        SetupProfileLookup(currentUserId, playerId);

        int targetId = 1;
        var initialCommand = new UpdateBestiaryUnlockCommand(targetId, 10, 1, 0, false);
        var expectedResponse = new UpdateBestiaryUnlockResponse(true, "Updated");

        _mediatorMock.Setup(m => m.Send(It.Is<UpdateBestiaryUnlockCommand>(c =>
                        c.Id == targetId &&
                        c.RequestingUserId == playerId &&
                        c.IsAdmin == true), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Update(targetId, initialCommand);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenProfileNotFound()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var command = new UpdateBestiaryUnlockCommand(1, 10, 1, 0, false);

        var result = await _controller.Update(1, command);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsOkResult_UsesPlayerIdFromProfile()
    {
        int targetId = 1;
        int currentUserId = 10;
        int playerId = 20;
        SetUserContext(currentUserId, isAdmin: false);
        SetupProfileLookup(currentUserId, playerId);

        var expectedResponse = new DeleteBestiaryUnlockResponse(true, "Deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeleteBestiaryUnlockCommand>(c =>
                        c.Id == targetId &&
                        c.RequestingUserId == playerId &&
                        c.IsAdmin == false), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Delete(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenProfileNotFound()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMine_ReturnsOk_WithUnlocks()
    {
        int currentUserId = 10;
        int playerId = 20;
        SetUserContext(currentUserId, isAdmin: false);
        SetupProfileLookup(currentUserId, playerId);

        var items = new List<BestiaryUnlock> { new BestiaryUnlock { Id = 1, PlayerId = playerId } };
        var expectedResponse = new GetAllBestiaryUnlocksResponse(items);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetAllBestiaryUnlocksQuery>(q => q.UserId == playerId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetMine();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMine_ReturnsNotFound_WhenProfileNotFound()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var result = await _controller.GetMine();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateForMe_ReturnsOk_WithCreatedUnlock()
    {
        int currentUserId = 10;
        int playerId = 20;
        int enemyId = 5;
        SetUserContext(currentUserId, isAdmin: false);
        SetupProfileLookup(currentUserId, playerId);

        var command = new CreateBestiaryUnlockForMeCommand(enemyId);
        var expectedResponse = new CreateBestiaryUnlockResponse(1, "Created");

        _mediatorMock.Setup(m => m.Send(
                It.Is<CreateBestiaryUnlockCommand>(c => c.PlayerId == playerId && c.EnemyId == enemyId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreateForMe(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CreateForMe_ReturnsNotFound_WhenProfileNotFound()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPlayerProfileByUserIdQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPlayerProfileByUserIdResponse?)null!);

        var result = await _controller.CreateForMe(new CreateBestiaryUnlockForMeCommand(1));

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
