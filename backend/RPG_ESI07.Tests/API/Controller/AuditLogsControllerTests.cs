using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Commands.AuditLogs;
using RPG_ESI07.Application.Queries.AuditLogs;

namespace RPG_ESI07.Tests.Controllers;

public class AuditLogsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new AuditLogsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var items = new List<AuditLogDto> { new AuditLogDto(1, null, "LOGIN_SUCCESS", DateTime.UtcNow, "127.0.0.1") };
        var expectedResponse = new GetAllAuditLogsResponse(items);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionResult_WithCorrectRouteAndData()
    {
        // Arrange
        // Instanciation via le constructeur primaire du record
        var command = new CreateAuditLogCommand(1, "UserLogin", "User 1 logged in");

        // Le type de retour attendu est désormais CreateAuditLogResponse
        var expectedResponse = new CreateAuditLogResponse(5, "Audit log created successfully");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Create(command);

        // Assert
        var createdAtResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtResult.StatusCode.Should().Be(201);
        createdAtResult.ActionName.Should().Be(nameof(AuditLogsController.GetAll));

        // Le routage utilise désormais expectedResponse.Id (provenant du nouveau record)
        createdAtResult.RouteValues.Should().NotBeNull();
        createdAtResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expectedResponse.Id);

        createdAtResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_ReturnsOkResult_WhenCommandSucceeds()
    {
        // Arrange
        int targetId = 10;

        // Remplacement de Unit.Value par DeleteAuditLogResponse
        var expectedResponse = new DeleteAuditLogResponse(true, "Audit log deleted");

        _mediatorMock.Setup(m => m.Send(It.Is<DeleteAuditLogCommand>(c => c.Id == targetId), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Delete(targetId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}