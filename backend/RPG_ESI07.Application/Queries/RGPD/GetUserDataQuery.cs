using MediatR;

namespace RPG_ESI07.Application.Queries.RGPD;

public record GetUserDataQuery(int UserId) : IRequest<GetUserDataResponse>;

public record GameSaveDto(
    int Id, string CurrentZone, float PositionX, float PositionY,
    string? InventoryData, string? QuestFlags, DateTime SavedAt);

public record InventoryItemDto(int Id, int ItemId, int Quantity);

public record SkillDto(int Id, int SkillId, int Level);

public record BestiaryUnlockDto(int Id, int EnemyId, DateTime UnlockedAt);

public record CombatStatsDto(
    int TotalCombats, int CombatsWon, int CombatsLost,
    long TotalDamageDealt, long TotalDamageTaken, int TotalPlaytimeMinutes);

public record GetUserDataResponse(
    int UserId,
    string Username,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    List<GameSaveDto> GameSaves,
    List<InventoryItemDto> Inventory,
    List<SkillDto> Skills,
    List<BestiaryUnlockDto> BestiaryUnlocks,
    CombatStatsDto? CombatStats);