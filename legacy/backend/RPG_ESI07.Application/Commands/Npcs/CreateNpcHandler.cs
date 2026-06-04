using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Npcs;

public class CreateNpcHandler : IRequestHandler<CreateNpcCommand, CreateNpcResponse>
{
    private readonly INpcRepository _repository;
    public CreateNpcHandler(INpcRepository repository) => _repository = repository;

    public async Task<CreateNpcResponse> Handle(CreateNpcCommand request, CancellationToken ct)
    {
        var entity = new Npc
        {
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            Zone = request.Zone,
            SpawnX = request.SpawnX,
            SpawnY = request.SpawnY,
            InfluenceRadius = request.InfluenceRadius,
            TransitionMatrix = request.TransitionMatrix,
            MapStates = request.MapStates,
            InitialState = request.InitialState,
            Dialogues = request.Dialogues,
            IsMerchant = request.IsMerchant,
            MerchantInventory = request.MerchantInventory,
            Quests = request.Quests
        };
        await _repository.AddAsync(entity);
        return new CreateNpcResponse(entity.Id, "Npc created successfully");
    }
}