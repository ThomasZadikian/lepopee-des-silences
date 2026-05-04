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
        var items = await _repository.GetAllAsync();

        if (request.UserId.HasValue)
        {
            items = items.Where(b => b != null && b.Player?.UserId == request.UserId.Value).ToList();
        }

        return new GetAllBestiaryUnlocksResponse(items);
    }
}