using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Npcs;

public class GetAllNpcsHandler : IRequestHandler<GetAllNpcsQuery, GetAllNpcsResponse>
{
    private readonly INpcRepository _repository;
    public GetAllNpcsHandler(INpcRepository repository) => _repository = repository;

    public async Task<GetAllNpcsResponse> Handle(GetAllNpcsQuery request, CancellationToken ct)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllNpcsResponse(items);
    }
}