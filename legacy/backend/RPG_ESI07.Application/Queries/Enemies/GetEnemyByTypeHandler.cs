using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Enemies;

public class GetEnemiesByTypeHandler
    : IRequestHandler<GetEnemiesByTypeQuery, GetEnemiesByTypeResponse>
{
    private readonly IEnemyRepository _repository;

    public GetEnemiesByTypeHandler(IEnemyRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEnemiesByTypeResponse> Handle(
        GetEnemiesByTypeQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _repository.GetByTypeAsync(request.Type);
        return new GetEnemiesByTypeResponse(items);
    }
}