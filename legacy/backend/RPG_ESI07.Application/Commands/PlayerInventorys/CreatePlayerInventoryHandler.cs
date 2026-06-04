using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerInventorys;

public class CreatePlayerInventoryHandler : IRequestHandler<CreatePlayerInventoryCommand, CreatePlayerInventoryResponse>
{
    private readonly IPlayerInventoryRepository _repository;

    public CreatePlayerInventoryHandler(IPlayerInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreatePlayerInventoryResponse> Handle(CreatePlayerInventoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new PlayerInventory();
        await _repository.AddAsync(entity);
        return new CreatePlayerInventoryResponse(entity.Id, "PlayerInventory created successfully");
    }
}