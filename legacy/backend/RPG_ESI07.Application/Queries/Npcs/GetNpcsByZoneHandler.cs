using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Npcs;

public class GetNpcsByZoneHandler : IRequestHandler<GetNpcsByZoneQuery, GetNpcsByZoneResponse>
{
    private readonly INpcRepository _repository;
    public GetNpcsByZoneHandler(INpcRepository repository) => _repository = repository;

    public async Task<GetNpcsByZoneResponse> Handle(GetNpcsByZoneQuery request, CancellationToken ct)
    {
        var items = await _repository.GetByZoneAsync(request.Zone);
        return new GetNpcsByZoneResponse(items);
    }
}
