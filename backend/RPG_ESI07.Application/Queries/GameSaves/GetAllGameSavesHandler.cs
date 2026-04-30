using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.GameSaves;

public class GetAllGameSavesHandler : IRequestHandler<GetAllGameSavesQuery, GetAllGameSavesResponse>
{
    private readonly IGameSaveRepository _repository;

    public GetAllGameSavesHandler(IGameSaveRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllGameSavesResponse> Handle(GetAllGameSavesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();

        if (request.UserId.HasValue)
            items = items.Where(s => s.PlayerId == request.UserId.Value).ToList();

        return new GetAllGameSavesResponse(items);
    }
}