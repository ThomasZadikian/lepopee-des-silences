using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Enemies;

public class GetAllEnemiesHandler
    : IRequestHandler<GetAllEnemiesQuery, GetAllEnemiesResponse>
{
    private readonly IEnemyRepository _repository;

    public GetAllEnemiesHandler(IEnemyRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllEnemiesResponse> Handle(
        GetAllEnemiesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllEnemiesResponse(items);
    }
}