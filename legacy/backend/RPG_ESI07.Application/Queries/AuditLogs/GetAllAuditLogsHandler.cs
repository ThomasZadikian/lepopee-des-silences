using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.AuditLogs;

public class GetAllAuditLogsHandler : IRequestHandler<GetAllAuditLogsQuery, GetAllAuditLogsResponse>
{
    private readonly IAuditLogRepository _repository;

    public GetAllAuditLogsHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllAuditLogsResponse> Handle(GetAllAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();

        var dtos = items.Select(a => new AuditLogDto(
            a.Id,
            a.UserId,
            a.EventType,
            a.Timestamp,
            a.IpAddress
        )).ToList();

        return new GetAllAuditLogsResponse(dtos);
    }
}