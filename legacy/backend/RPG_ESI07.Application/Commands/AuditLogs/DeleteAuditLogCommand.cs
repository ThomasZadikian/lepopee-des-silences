using MediatR;

namespace RPG_ESI07.Application.Commands.AuditLogs;

public record DeleteAuditLogCommand(int Id) : IRequest<DeleteAuditLogResponse>;

public record DeleteAuditLogResponse(bool Success, string Message);