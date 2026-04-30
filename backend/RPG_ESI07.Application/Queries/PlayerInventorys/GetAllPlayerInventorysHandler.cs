using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.PlayerInventorys;

public class GetAllPlayerInventorysHandler : IRequestHandler<GetAllPlayerInventorysQuery, GetAllPlayerInventorysResponse>
{
    private readonly IPlayerInventoryRepository _repository;

    public GetAllPlayerInventorysHandler(IPlayerInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllPlayerInventorysResponse> Handle(GetAllPlayerInventorysQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();

        if (request.UserId.HasValue)
            items = items.Where(i => i.PlayerId == request.UserId.Value).ToList();

        return new GetAllPlayerInventorysResponse(items);
    }
}