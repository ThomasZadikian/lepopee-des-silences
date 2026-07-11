namespace Leds.GameEngine.Application.Players.Ports;

public interface IPlayerProfileGateway
{
    Task AwardStatPointAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> EquipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnequipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> AddPermanentItemsAsync(Guid playerId, IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, CancellationToken cancellationToken);

    Task<PlayerProfileView> SetPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, string liquidDefinitionKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> ClearPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken, string source = "devtools");

    Task<PlayerProfileView> AwardStatPointsAsync(Guid playerId, int amount, CancellationToken cancellationToken);

    Task<bool> HasClaimedNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, CancellationToken cancellationToken);

    Task ClaimNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, Guid? sourceRunId, CancellationToken cancellationToken);

    Task GrantReputationMilestoneAsync(Guid playerId, string npcKey, string milestoneKey, Guid? sourceRunId, CancellationToken cancellationToken);

    /// <summary>Recruits an NPC as a permanent companion — added to the player's roster
    /// for life, fights alongside the protagonist in every future run.</summary>
    Task<PlayerProfileView> RecruitCompanionAsync(
        Guid playerId, string companionDefinitionKey, string displayName,
        int maxVitality, int attackPower, int defense, int startingGuard,
        int speed, int initiative, int recovery, int focus, int mana, int charge,
        IReadOnlyCollection<string> skillKeys, CancellationToken cancellationToken);
}
