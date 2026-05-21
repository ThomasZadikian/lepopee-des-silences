using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.Skills;
using RPG_ESI07.Application.Queries.Skills;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Tests.Controllers;

public class SkillsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SkillsController _controller;

    public SkillsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new SkillsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithExpectedData()
    {
        var items = new List<Skill> { new Skill { Id = 1 } };
        var expectedResponse = new GetAllSkillsResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllSkillsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenCommandSucceeds()
    {
        var command = new CreateSkillCommand("Fireball", "Damage", 10, "fire", "Boule de feu", 80, 0);
        var expectedResponse = new CreateSkillResponse(1, "Skill created");

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
        var command = new UpdateSkillCommand(2, "Ice Spike", "Damage", 15, "ice", "Pique de glace", 70, 0);

        var result = await _controller.Update(1, command);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Id mismatch");
    }

    [Fact]
    public async Task Update_ReturnsOkResult_WhenCommandSucceeds()
    {
        int targetId = 1;
        var command = new UpdateSkillCommand(targetId, "Ice Spike", "Damage", 15, "ice", "Pique de glace", 70, 0);
        var expectedResponse = new UpdateSkillResponse(true, "Skill updated");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Update(targetId, command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_ReturnsOkResult_WhenCommandSucceeds()
    {
        int targetId = 1;
        var expectedResponse = new DeleteSkillResponse(true, "Skill deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeleteSkillCommand>(c => c.Id == targetId), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        var result = await _controller.Delete(targetId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}