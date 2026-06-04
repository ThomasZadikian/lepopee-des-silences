using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.AuditLogs;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.AuditLogs;

public class CreateAuditLogHandlerTests
{
    private readonly Mock<IAuditLogRepository> _mockRepo;
    private readonly CreateAuditLogHandler _handler;

    public CreateAuditLogHandlerTests()
    {
        _mockRepo = new Mock<IAuditLogRepository>();
        _handler = new CreateAuditLogHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreateAuditLogCommand(1, "LOGIN_SUCCESS", "{}");
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("AuditLog created successfully");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }
}

public class DeleteAuditLogHandlerTests
{
    private readonly Mock<IAuditLogRepository> _mockRepo;
    private readonly DeleteAuditLogHandler _handler;

    public DeleteAuditLogHandlerTests()
    {
        _mockRepo = new Mock<IAuditLogRepository>();
        _handler = new DeleteAuditLogHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsSuccessResponse()
    {
        // Arrange
        _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteAuditLogCommand(1), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}