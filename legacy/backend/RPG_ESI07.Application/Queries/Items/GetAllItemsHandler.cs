using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Items;

public class GetAllItemsHandler : IRequestHandler<GetAllItemsQuery, GetAllItemsResponse>
{
    private readonly IItemRepository _repository;

    public GetAllItemsHandler(IItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllItemsResponse> Handle(GetAllItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllItemsResponse(items);
    }
}