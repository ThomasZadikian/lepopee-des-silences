using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Enemies;

public class GetEnemyByIdHandler
    : IRequestHandler<GetEnemyByIdQuery, GetEnemyByIdResponse>
{
    private readonly IEnemyRepository _repository;

    public GetEnemyByIdHandler(IEnemyRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEnemyByIdResponse> Handle(
        GetEnemyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var enemy = await _repository.GetByIdAsync(request.Id);

        if (enemy == null)
            throw new KeyNotFoundException($"Ennemi {request.Id} introuvable.");

        return new GetEnemyByIdResponse(enemy);
    }
}