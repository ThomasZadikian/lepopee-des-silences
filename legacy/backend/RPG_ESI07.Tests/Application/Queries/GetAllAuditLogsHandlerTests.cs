using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.AuditLogs;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllAuditLogsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        var items = new List<AuditLog> { new() { Id = 1 }, new() { Id = 2 } };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        var handler = new GetAllAuditLogsHandler(mockRepo.Object);

        var result = await handler.Handle(new GetAllAuditLogsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>());
        var handler = new GetAllAuditLogsHandler(mockRepo.Object);

        var result = await handler.Handle(new GetAllAuditLogsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DoesNotExposeEventData()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        var items = new List<AuditLog>
        {
            new() { Id = 1, EventType = "LOGIN_SUCCESS", EventData = "sensitive_data", IpAddress = "127.0.0.1", Timestamp = DateTime.UtcNow }
        };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        var handler = new GetAllAuditLogsHandler(mockRepo.Object);

        var result = await handler.Handle(new GetAllAuditLogsQuery(), CancellationToken.None);

        var dto = result.Items.First();
        dto.Should().BeOfType<AuditLogDto>();
        var dtoProperties = typeof(AuditLogDto).GetProperties().Select(p => p.Name);
        dtoProperties.Should().NotContain("EventData");
    }
}