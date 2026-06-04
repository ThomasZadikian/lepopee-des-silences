using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.UserConsents;
using RPG_ESI07.Application.Queries.UserConsents;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using System.Security.Claims;

namespace RPG_ESI07.Tests.Controllers;

public class UserConsentsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UserConsentsController _controller;

    public UserConsentsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new UserConsentsController(_mediatorMock.Object);
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
        var items = new List<UserConsent> { new UserConsent { Id = 1 } };
        var expectedResponse = new GetAllUserConsentsResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUserConsentsQuery>(), It.IsAny<CancellationToken>()))
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

        var expectedConsent = new UserConsent { Id = targetId };
        var expectedResponse = new GetUserConsentByIdResponse(expectedConsent);

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserConsentByIdQuery>(q =>
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
        int targetUserId = 15;
        SetUserContext(currentUserId, isAdmin: false);

        var command = new CreateUserConsentCommand(targetUserId, true, false);

        var result = await _controller.Create(command);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenSelfCreationSucceeds()
    {
        int currentUserId = 10;
        SetUserContext(currentUserId, isAdmin: false);

        var command = new CreateUserConsentCommand(currentUserId, true, true);
        var expectedResponse = new CreateUserConsentResponse(1, "Created");

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
        var command = new UpdateUserConsentCommand(2, 10, true, false, 0, false);

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
        var initialCommand = new UpdateUserConsentCommand(targetId, 10, true, true, 0, false);
        var expectedResponse = new UpdateUserConsentResponse(true, "Updated");

        _mediatorMock.Setup(m => m.Send(It.Is<UpdateUserConsentCommand>(c =>
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

        var expectedResponse = new DeleteUserConsentResponse(true, "Deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeleteUserConsentCommand>(c =>
                        c.Id == targetId &&
                        c.RequestingUserId == currentUserId &&
                        c.IsAdmin == false), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Delete(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}