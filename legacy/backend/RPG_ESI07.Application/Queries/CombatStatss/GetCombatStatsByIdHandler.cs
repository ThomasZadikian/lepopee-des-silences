using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.CombatStatss;

public class GetCombatStatsByIdHandler : IRequestHandler<GetCombatStatsByIdQuery, GetCombatStatsByIdResponse>
{
    private readonly ICombatStatsRepository _repository;

    public GetCombatStatsByIdHandler(ICombatStatsRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetCombatStatsByIdResponse> Handle(GetCombatStatsByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("CombatStats not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetCombatStatsByIdResponse(entity);
    }
}