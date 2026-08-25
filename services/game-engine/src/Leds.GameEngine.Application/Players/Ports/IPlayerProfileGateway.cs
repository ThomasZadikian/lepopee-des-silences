namespace Leds.GameEngine.Application.Players.Ports;

public interface IPlayerProfileGateway
{
    Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> EquipItemAsync(
        Guid playerId,
        Guid characterId,
        string itemKey,
        string slot,
        CancellationToken cancellationToken);

    Task<PlayerProfileView> UnequipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> AddPermanentItemsAsync(Guid playerId, IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, CancellationToken cancellationToken);

    Task<PlayerProfileView> SetPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, string liquidDefinitionKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> ClearPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken, string source = "devtools");

    /// <summary>Awards a flat amount of the player's persistent currency ("Éclats du Palais").</summary>
    Task<PlayerProfileView> AwardCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken);

    /// <summary>Spends the player's persistent currency ("Éclats du Palais") if they can
    /// afford it. Returns false (not an exception) on insufficient funds — insolvency is
    /// an expected outcome for callers like "Loi de l'Impôt du Seuil".</summary>
    Task<bool> TrySpendCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken);

    /// <summary>Awards a flat amount of the player's second persistent currency
    /// ("Éclats de Him'Lit") — earned only from Périlleux/Fatal-tier combat victories.
    /// Mirrors AwardCurrencyAsync exactly (separate currency, not a generalized type).</summary>
    Task<PlayerProfileView> AwardHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken);

    /// <summary>Spends "Éclats de Him'Lit" if the player can afford it. Mirrors
    /// TrySpendCurrencyAsync exactly.</summary>
    Task<bool> TrySpendHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken);

    Task<bool> HasClaimedNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, CancellationToken cancellationToken);

    Task ClaimNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, Guid? sourceRunId, CancellationToken cancellationToken);

    Task GrantReputationMilestoneAsync(Guid playerId, string npcKey, string milestoneKey, Guid? sourceRunId, CancellationToken cancellationToken);

    /// <summary>Recruits an NPC as a permanent companion — added to the player's roster
    /// for life, fights alongside the protagonist in every future run.</summary>
    Task<PlayerProfileView> RecruitCompanionAsync(
        Guid playerId, string companionDefinitionKey, string displayName,
        int maxVitality, int attackPower, int defense, int startingGuard,
        int speed, int initiative, int focus, int mana, int charge,
        IReadOnlyCollection<string> skillKeys,
        CancellationToken cancellationToken,
        int magicAttack = 0, int magicDefense = 0);

    Task<IReadOnlyCollection<NpcReputationScoreView>> GetNpcReputationScoresAsync(Guid playerId, CancellationToken cancellationToken);

    Task UpsertNpcReputationScoresAsync(Guid playerId, Guid sourceRunId, IReadOnlyCollection<NpcReputationScoreView> scores, CancellationToken cancellationToken);

    Task<PlayerProfileView> AdvanceMainStoryAsync(Guid playerId, MainStoryAdvanceView progress, CancellationToken cancellationToken);

    Task<PlayerProfileView> UnlockDifficultyLevelAsync(Guid playerId, int level, CancellationToken cancellationToken);
}

public sealed record NpcReputationScoreView(
    string NpcKey,
    int Score,
    int TimesMet,
    string? CurrentDialogueNodeKey);

public sealed record MainStoryAdvanceView(
    string SequenceKey,
    string SequenceVersion,
    string StepKey,
    string? CheckpointKey,
    IReadOnlyCollection<string> UnlockedRoomKeys,
    IReadOnlyCollection<string> VisibleRoomKeys,
    bool Complete);
