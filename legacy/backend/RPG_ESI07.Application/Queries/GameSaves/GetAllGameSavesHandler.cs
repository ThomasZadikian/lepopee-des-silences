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
        var items = request.UserId.HasValue
            ? await _repository.GetByPlayerIdAsync(request.UserId.Value)
            : await _repository.GetAllAsync();

        return new GetAllGameSavesResponse(items);
    }
}