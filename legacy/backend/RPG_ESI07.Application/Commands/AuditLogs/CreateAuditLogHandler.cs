using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.AuditLogs;

public class CreateAuditLogHandler : IRequestHandler<CreateAuditLogCommand, CreateAuditLogResponse>
{
    private readonly IAuditLogRepository _repository;

    public CreateAuditLogHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateAuditLogResponse> Handle(CreateAuditLogCommand request, CancellationToken cancellationToken)
    {
        var entity = new AuditLog();
        await _repository.AddAsync(entity);
        return new CreateAuditLogResponse(entity.Id, "AuditLog created successfully");
    }
}