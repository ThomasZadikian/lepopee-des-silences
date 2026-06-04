using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.Npcs;
using RPG_ESI07.Application.Queries.Npcs;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Tests.Controllers;

public class NpcsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly NpcsController _controller;

    public NpcsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new NpcsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithNpcList()
    {
        var npcs = new List<Npc> { new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral } };
        var expected = new GetAllNpcsResponse(npcs);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllNpcsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.GetAll();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenNpcExists()
    {
        var npc = new Npc { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        var expected = new GetNpcByIdResponse(npc);
        _mediatorMock.Setup(m => m.Send(It.Is<GetNpcByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.GetById(1);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByZone_ReturnsOk_WithFilteredNpcs()
    {
        var npcs = new List<Npc> { new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral } };
        var expected = new GetNpcsByZoneResponse(npcs);
        _mediatorMock.Setup(m => m.Send(It.Is<GetNpcsByZoneQuery>(q => q.Zone == "Dev Island"), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.GetByZone("Dev Island");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByType_ReturnsOk_WithFilteredNpcs()
    {
        var npcs = new List<Npc> { new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral } };
        var expected = new GetNpcsByTypeResponse(npcs);
        _mediatorMock.Setup(m => m.Send(It.Is<GetNpcsByTypeQuery>(q => q.Type == Constants.NpcTypeNeutral), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.GetByType(Constants.NpcTypeNeutral);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenCommandSucceeds()
    {
        var command = new CreateNpcCommand("Aldric", Constants.NpcTypeNeutral, null, "Dev Island", 0.05f, 0.57f, 5.0f, null, null, Constants.NpcStateSerein, null, false, null, null);
        var expected = new CreateNpcResponse(1, "Npc created successfully");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Create(command);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdMismatch()
    {
        var command = new UpdateNpcCommand(2, "Aldric", Constants.NpcTypeNeutral, null, "Dev Island", 0.05f, 0.57f, 5.0f, null, null, Constants.NpcStateSerein, null, false, null, null);

        var result = await _controller.Update(1, command);

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Id mismatch");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenCommandSucceeds()
    {
        var command = new UpdateNpcCommand(1, "Aldric", Constants.NpcTypeNeutral, null, "Dev Island", 0.05f, 0.57f, 5.0f, null, null, Constants.NpcStateSerein, null, false, null, null);
        var expected = new UpdateNpcResponse(true, "Npc updated successfully");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Update(1, command);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenCommandSucceeds()
    {
        var expected = new DeleteNpcResponse(true, "Npc deleted successfully");
        _mediatorMock.Setup(m => m.Send(It.Is<DeleteNpcCommand>(c => c.Id == 1), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }
}