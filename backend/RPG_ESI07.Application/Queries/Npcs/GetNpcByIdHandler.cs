using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Npcs;

public class GetNpcByIdHandler : IRequestHandler<GetNpcByIdQuery, GetNpcByIdResponse>
{
    private readonly INpcRepository _repository;
    public GetNpcByIdHandler(INpcRepository repository) => _repository = repository;

    public async Task<GetNpcByIdResponse> Handle(GetNpcByIdQuery request, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(request.Id);
        return new GetNpcByIdResponse(item);
    }
}