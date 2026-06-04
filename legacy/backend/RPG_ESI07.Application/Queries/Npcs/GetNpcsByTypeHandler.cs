using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Npcs;

public class GetNpcsByTypeHandler : IRequestHandler<GetNpcsByTypeQuery, GetNpcsByTypeResponse>
{
    private readonly INpcRepository _repository;
    public GetNpcsByTypeHandler(INpcRepository repository) => _repository = repository;

    public async Task<GetNpcsByTypeResponse> Handle(GetNpcsByTypeQuery request, CancellationToken ct)
    {
        var items = await _repository.GetByTypeAsync(request.Type);
        return new GetNpcsByTypeResponse(items);
    }
}