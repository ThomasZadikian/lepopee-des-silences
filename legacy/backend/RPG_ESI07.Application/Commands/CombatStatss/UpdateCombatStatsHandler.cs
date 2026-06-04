using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public class UpdateCombatStatsHandler : IRequestHandler<UpdateCombatStatsCommand, UpdateCombatStatsResponse>
{
    private readonly ICombatStatsRepository _repository;

    public UpdateCombatStatsHandler(ICombatStatsRepository repository) => _repository = repository;

    public async Task<UpdateCombatStatsResponse> Handle(UpdateCombatStatsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("CombatStats not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        entity.PlayerId = request.PlayerId;
        entity.TotalCombats = request.TotalCombats;
        entity.CombatsWon = request.CombatsWon;
        entity.CombatsLost = request.CombatsLost;
        entity.TotalDamageDealt = request.TotalDamageDealt;
        entity.TotalDamageTaken = request.TotalDamageTaken;
        entity.TotalPlaytimeMinutes = request.TotalPlaytimeMinutes;
        await _repository.UpdateAsync(entity);
        return new UpdateCombatStatsResponse(true, "CombatStats updated successfully");
    }
}