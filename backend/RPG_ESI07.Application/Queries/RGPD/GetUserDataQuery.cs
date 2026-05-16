using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.RGPD;

public record GetUserDataQuery(int UserId) : IRequest<GetUserDataResponse>;

public record GetUserDataResponse(
    int UserId,
    string Username,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? LastLoginIP,
    List<GameSave> GameSaves,
    List<PlayerInventory> Inventory,
    List<PlayerSkill> Skills,
    List<BestiaryUnlock> BestiaryUnlocks,
    CombatStats? CombatStats
    );