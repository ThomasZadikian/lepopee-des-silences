using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.BestiaryUnlocks;

public class GetAllBestiaryUnlocksHandler : IRequestHandler<GetAllBestiaryUnlocksQuery, GetAllBestiaryUnlocksResponse>
{
    private readonly IBestiaryUnlockRepository _repository;

    public GetAllBestiaryUnlocksHandler(IBestiaryUnlockRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllBestiaryUnlocksResponse> Handle(GetAllBestiaryUnlocksQuery request, CancellationToken cancellationToken)
    {
        var items = request.UserId.HasValue
            ? await _repository.GetByPlayerIdAsync(request.UserId.Value)
            : await _repository.GetAllAsync();

        return new GetAllBestiaryUnlocksResponse(items);
    }
}