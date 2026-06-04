using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.GameSaves;

public class GetSaveByIdHandler : IRequestHandler<GetSaveByIdQuery, GetSaveByIdResponse>
{
    private readonly IGameSaveRepository _repository;

    public GetSaveByIdHandler(IGameSaveRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetSaveByIdResponse> Handle(GetSaveByIdQuery request, CancellationToken cancellationToken)
    {
        var save = await _repository.GetByIdAsync(request.Id);
        if (save == null)
        {
            throw new InvalidOperationException();
        }

        if (!request.IsAdmin && save.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetSaveByIdResponse(save);
    }
}