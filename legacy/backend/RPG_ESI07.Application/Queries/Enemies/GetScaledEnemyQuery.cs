using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Enemies;

public record GetScaledEnemyQuery(
    int EnemyId,
    int PlayerLevel,
    int EquipmentBonus
) : IRequest<GetScaledEnemyResponse>;

public record GetScaledEnemyResponse(ScaledEnemyDto? Enemy);

public record ScaledEnemyDto(
    int Id,
    string Name,
    string Type,
    int ScaledMaxHP,
    int ScaledStrength,
    int ScaledIntelligence,
    int ScaledSpeed,
    float PhysicalResistance,
    float MagicalResistance,
    int ExperienceReward,
    int GoldReward,
    string? Description,
    string? TransitionMatrix,
    string? CombatScripts,
    string? MapStates,
    string InitialState,
    float Multiplier
);