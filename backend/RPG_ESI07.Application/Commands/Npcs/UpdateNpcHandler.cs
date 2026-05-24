using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Npcs;
public class UpdateNpcHandler : IRequestHandler<UpdateNpcCommand, UpdateNpcResponse>
{
    private readonly INpcRepository _repository;
    public UpdateNpcHandler(INpcRepository repository) => _repository = repository;

    public async Task<UpdateNpcResponse> Handle(UpdateNpcCommand request, CancellationToken ct)
    {
        var entity = new Npc
        {
            Id                = request.Id,
            Name              = request.Name,
            Type              = request.Type,
            Description       = request.Description,
            Zone              = request.Zone,
            SpawnX            = request.SpawnX,
            SpawnY            = request.SpawnY,
            InfluenceRadius   = request.InfluenceRadius,
            TransitionMatrix  = request.TransitionMatrix,
            MapStates         = request.MapStates,
            InitialState      = request.InitialState,
            Dialogues         = request.Dialogues,
            IsMerchant        = request.IsMerchant,
            MerchantInventory = request.MerchantInventory,
            Quests            = request.Quests
        };
        await _repository.UpdateAsync(entity);
        return new UpdateNpcResponse(true, "Npc updated successfully");
    }
}