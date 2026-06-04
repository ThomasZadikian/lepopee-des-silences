using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.CombatStatss;

public class GetAllCombatStatssHandler : IRequestHandler<GetAllCombatStatssQuery, GetAllCombatStatssResponse>
{
    private readonly ICombatStatsRepository _repository;

    public GetAllCombatStatssHandler(ICombatStatsRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllCombatStatssResponse> Handle(GetAllCombatStatssQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllCombatStatssResponse(items);
    }
}