using MediatR;

namespace RPG_ESI07.Application.Commands.Enemies;

public record CreateEnemyCommand(
    string Name,
    string Type,
    int MaxHP,
    int Strength,
    int Intelligence,
    int Speed,
    float PhysicalResistance,
    float MagicalResistance,
    int ExperienceReward,
    int GoldReward,
    string? Description,
    float InfluenceRadius,
    string? TransitionMatrix,
    string? CombatScripts,
    string? MapStates,
    string InitialState
) : IRequest<CreateEnemyResponse>;

public record CreateEnemyResponse(int Id, string Message);