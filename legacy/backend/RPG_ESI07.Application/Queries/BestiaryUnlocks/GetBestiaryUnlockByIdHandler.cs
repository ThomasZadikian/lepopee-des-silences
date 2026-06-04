using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.BestiaryUnlocks;

public class GetBestiaryUnlockByIdHandler : IRequestHandler<GetBestiaryUnlockByIdQuery, GetBestiaryUnlockByIdResponse>
{
    private readonly IBestiaryUnlockRepository _repository;

    public GetBestiaryUnlockByIdHandler(IBestiaryUnlockRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetBestiaryUnlockByIdResponse> Handle(GetBestiaryUnlockByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("BestiaryUnlock not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetBestiaryUnlockByIdResponse(entity);
    }
}