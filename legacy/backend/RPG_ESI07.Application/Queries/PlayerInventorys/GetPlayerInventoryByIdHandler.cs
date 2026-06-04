using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.PlayerInventorys;

public class GetPlayerInventoryByIdHandler : IRequestHandler<GetPlayerInventoryByIdQuery, GetPlayerInventoryByIdResponse>
{
    private readonly IPlayerInventoryRepository _repository;

    public GetPlayerInventoryByIdHandler(IPlayerInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPlayerInventoryByIdResponse> Handle(GetPlayerInventoryByIdQuery request, CancellationToken cancellationToken)
    {
        var playerInventory = await _repository.GetByIdAsync(request.Id);
        if (playerInventory == null)
            throw new KeyNotFoundException("Player inventory not found");

        if (!request.IsAdmin && playerInventory.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetPlayerInventoryByIdResponse(playerInventory);
    }
}