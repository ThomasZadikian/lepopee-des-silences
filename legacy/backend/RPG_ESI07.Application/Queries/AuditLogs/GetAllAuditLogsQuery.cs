using MediatR;

namespace RPG_ESI07.Application.Queries.AuditLogs;

public record GetAllAuditLogsQuery : IRequest<GetAllAuditLogsResponse>;

public record GetAllAuditLogsResponse(List<AuditLogDto> Items);

public record AuditLogDto(
    int Id,
    int? UserId,
    string EventType,
    DateTime Timestamp,
    string? IpAddress
);