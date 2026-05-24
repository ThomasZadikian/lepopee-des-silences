using MediatR;

namespace RPG_ESI07.Application.Commands.Npcs;
public record CreateNpcCommand(
    string Name,
    string Type,
    string? Description,
    string Zone,
    float SpawnX,
    float SpawnY,
    float InfluenceRadius,
    string? TransitionMatrix,
    string? MapStates,
    string InitialState,
    string? Dialogues,
    bool IsMerchant,
    string? MerchantInventory,
    string? Quests
) : IRequest<CreateNpcResponse>;

public record CreateNpcResponse(int Id, string Message);