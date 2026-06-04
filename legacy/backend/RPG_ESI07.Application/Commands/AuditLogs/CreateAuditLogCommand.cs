using MediatR;

namespace RPG_ESI07.Application.Commands.AuditLogs;

public record CreateAuditLogCommand(
    int UserId, string EventType, string EventData
) : IRequest<CreateAuditLogResponse>;

public record CreateAuditLogResponse(int Id, string Message);