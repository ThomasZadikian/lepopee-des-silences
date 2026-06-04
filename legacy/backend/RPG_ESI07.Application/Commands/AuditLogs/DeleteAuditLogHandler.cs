using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.AuditLogs;

public class DeleteAuditLogHandler : IRequestHandler<DeleteAuditLogCommand, DeleteAuditLogResponse>
{
    private readonly IAuditLogRepository _repository;

    public DeleteAuditLogHandler(IAuditLogRepository repository) => _repository = repository;

    public async Task<DeleteAuditLogResponse> Handle(DeleteAuditLogCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteAuditLogResponse(true, "AuditLog deleted successfully");
    }
}