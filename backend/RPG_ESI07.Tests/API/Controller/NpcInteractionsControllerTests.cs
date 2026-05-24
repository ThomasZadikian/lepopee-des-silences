using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.NpcInteractions;
using RPG_ESI07.Domain;

namespace RPG_ESI07.Tests.Controllers;

public class NpcInteractionsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly NpcInteractionsController _controller;

    public NpcInteractionsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new NpcInteractionsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenCommandSucceeds()
    {
        var command = new CreateNpcInteractionCommand(1, 1, "DIALOGUE");
        var expected = new CreateNpcInteractionResponse(1, "NpcInteraction created successfully");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Create(command);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenCommandSucceeds()
    {
        var expected = new DeleteNpcInteractionResponse(true, "NpcInteraction deleted successfully");
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteNpcInteractionCommand>(c => c.Id == 1), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }
}