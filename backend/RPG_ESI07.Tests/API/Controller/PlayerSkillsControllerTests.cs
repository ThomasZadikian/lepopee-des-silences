using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.PlayerSkills;
using RPG_ESI07.Application.Queries.PlayerSkills;
using RPG_ESI07.Domain.Entities;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class PlayerSkillsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly PlayerSkillsController _controller;

    public PlayerSkillsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new PlayerSkillsController(_mediatorMock.Object);
    }

    private void SetUserContext(int userId, bool isAdmin = false)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithExpectedData()
    {
        var items = new List<PlayerSkill> { new PlayerSkill { Id = 1 } };
        var expectedResponse = new GetAllPlayerSkillsResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllPlayerSkillsQuery>(), It.IsAny<CancellationToken>()))
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

        var expectedSkill = new PlayerSkill { Id = targetId };
        var expectedResponse = new GetPlayerSkillByIdResponse(expectedSkill);

        _mediatorMock.Setup(m => m.Send(It.Is<GetPlayerSkillByIdQuery>(q =>
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

        var command = new CreatePlayerSkillCommand(targetPlayerId, 1, 5);

        var result = await _controller.Create(command);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenSelfCreationSucceeds()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var command = new CreatePlayerSkillCommand(currentUserId, 1, 5);
        var expectedResponse = new CreatePlayerSkillResponse(1, "Created");

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
        var command = new UpdatePlayerSkillCommand(2, 10, 1, 0, false);

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
        var initialCommand = new UpdatePlayerSkillCommand(targetId, 10, 1, 0, false);
        var expectedResponse = new UpdatePlayerSkillResponse(true, "Updated");

        _mediatorMock.Setup(m => m.Send(It.Is<UpdatePlayerSkillCommand>(c =>
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

        var expectedResponse = new DeletePlayerSkillResponse(true, "Deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeletePlayerSkillCommand>(c =>
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
    public async Task GetMySkills_ReturnsOk_WithPlayerSkills()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var items = new List<PlayerSkill> { new PlayerSkill { Id = 1, PlayerId = currentUserId } };
        var expectedResponse = new GetAllPlayerSkillsResponse(items);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetAllPlayerSkillsQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetMySkills();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMySkills_ExtractsUserIdFromJwt()
    {
        int currentUserId = 42;
        SetUserContext(currentUserId);

        var expectedResponse = new GetAllPlayerSkillsResponse(new List<PlayerSkill>());

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetAllPlayerSkillsQuery>(q => q.UserId == currentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        await _controller.GetMySkills();

        _mediatorMock.Verify(m => m.Send(
                It.Is<GetAllPlayerSkillsQuery>(q => q.UserId == 42),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}