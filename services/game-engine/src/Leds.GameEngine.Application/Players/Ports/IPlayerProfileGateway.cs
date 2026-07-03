namespace Leds.GameEngine.Application.Players.Ports;

public interface IPlayerProfileGateway
{
    Task AwardStatPointAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> AwardStatPointsAsync(Guid playerId, int amount, CancellationToken cancellationToken);
}
