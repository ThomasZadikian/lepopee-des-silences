using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public class DeleteCombatStatsHandler : IRequestHandler<DeleteCombatStatsCommand, DeleteCombatStatsResponse>
{
    private readonly ICombatStatsRepository _repository;

    public DeleteCombatStatsHandler(ICombatStatsRepository repository) => _repository = repository;

    public async Task<DeleteCombatStatsResponse> Handle(DeleteCombatStatsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("CombatStats not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        await _repository.DeleteAsync(request.Id);
        return new DeleteCombatStatsResponse(true, "CombatStats deleted successfully");
    }
}