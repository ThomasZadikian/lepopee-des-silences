using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.GameSaves;
using RPG_ESI07.Application.Queries.GameSaves;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class GameSavesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GameSavesController _controller;

    public GameSavesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new GameSavesController(_mediatorMock.Object);
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

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithExpectedData()
    {
        var items = new List<GameSave> { new GameSave { Id = 1 } };
        var expectedResponse = new GetAllGameSavesResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllGameSavesQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_ExtractsClaimsCorrectly()
    {
        int targetId = 5;
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var expectedSave = new GameSave { Id = targetId };
        var expectedResponse = new GetSaveByIdResponse(expectedSave);

        _mediatorMock.Setup(m => m.Send(It.Is<GetSaveByIdQuery>(q =>
                        q.Id == targetId &&
                        q.RequestingUserId == currentUserId &&
                        q.IsAdmin == false), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.GetById(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_ReturnsForbid_WhenNonAdminCreatesForAnotherUser()
    {
        int currentUserId = 10;
        int targetPlayerId = 15;
        SetUserContext(currentUserId, isAdmin: false);

        var command = new CreateGameSaveCommand(targetPlayerId, "Zone1", 5);

        var result = await _controller.Create(command);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenSelfCreationSucceeds()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var command = new CreateGameSaveCommand(currentUserId, "Zone1", 5);
        var expectedResponse = new CreateGameSaveResponse(1, "Created");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Create(command);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expectedResponse.Id);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdMismatch()
    {
        var command = new UpdateGameSaveCommand(2, 10, "Zone1", 5, 0, false);

        var result = await _controller.Update(1, command);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_ReturnsOkResult_AppliesClaimsToCommand()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: true);

        int targetId = 1;
        var initialCommand = new UpdateGameSaveCommand(targetId, 10, "Zone2", 6, 0, false);
        var expectedResponse = new UpdateGameSaveResponse(true, "Updated");

        _mediatorMock.Setup(m => m.Send(It.Is<UpdateGameSaveCommand>(c =>
                        c.Id == targetId &&
                        c.RequestingUserId == currentUserId &&
                        c.IsAdmin == true), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Update(targetId, initialCommand);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_ReturnsOkResult_ExtractsClaimsCorrectly()
    {
        int targetId = 1;
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var expectedResponse = new DeleteGameSaveResponse(true, "Deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeleteGameSaveCommand>(c =>
                        c.Id == targetId &&
                        c.RequestingUserId == currentUserId &&
                        c.IsAdmin == false), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Delete(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMySaves_ReturnsOk_WithPlayerSaves()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var items = new List<GameSave> { new GameSave { Id = 1, PlayerId = currentUserId } };
        var expectedResponse = new GetAllGameSavesResponse(items);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetAllGameSavesQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetMySaves();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMySaves_ExtractsUserIdFromJwt()
    {
        int currentUserId = 42;
        SetUserContext(currentUserId);

        var expectedResponse = new GetAllGameSavesResponse(new List<GameSave>());

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetAllGameSavesQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        await _controller.GetMySaves();

        _mediatorMock.Verify(m => m.Send(
                It.Is<GetAllGameSavesQuery>(q => q.UserId == 42),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}