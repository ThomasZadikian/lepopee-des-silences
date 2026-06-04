using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerInventorys;

public class DeletePlayerInventoryHandler : IRequestHandler<DeletePlayerInventoryCommand, DeletePlayerInventoryResponse>
{
    private readonly IPlayerInventoryRepository _repository;

    public DeletePlayerInventoryHandler(IPlayerInventoryRepository repository) => _repository = repository;

    public async Task<DeletePlayerInventoryResponse> Handle(DeletePlayerInventoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("PlayerInventory not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        await _repository.DeleteAsync(request.Id);
        return new DeletePlayerInventoryResponse(true, "PlayerInventory deleted successfully");
    }
}