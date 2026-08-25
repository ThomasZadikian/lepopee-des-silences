using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.UnitTests.Common;

/// <summary>
/// In-memory fake for <see cref="IPlayerProfileGateway"/>. Records every call so tests
/// can assert on what was granted/claimed, and lets tests pre-seed claimed offerings to
/// exercise the "never given twice" invariant.
/// </summary>
public sealed class StubPlayerProfileGateway : IPlayerProfileGateway
{
    private readonly HashSet<string> _claimedOfferings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, int> _currencyBalances = new();
    private readonly Dictionary<Guid, int> _himLitCurrencyBalances = new();

    /// <summary>When true, <see cref="TrySpendHimLitCurrencyAsync"/> always fails
    /// regardless of balance — simulates a player-service call that fails for reasons
    /// other than a stale affordability pre-check (used to test refund-on-partial-failure
    /// logic in callers that spend two currencies non-atomically).</summary>
    public bool ForceHimLitSpendFailure { get; set; }

    public List<(Guid PlayerId, Guid CharacterId, string SkillKey, string Source)> UnlockedSkills { get; } = [];
    public List<(Guid PlayerId, int Amount)> AwardedCurrency { get; } = [];
    public List<(Guid PlayerId, int Amount, bool Succeeded)> SpentCurrencyAttempts { get; } = [];
    public List<(Guid PlayerId, int Amount)> AwardedHimLitCurrency { get; } = [];
    public List<(Guid PlayerId, int Amount, bool Succeeded)> SpentHimLitCurrencyAttempts { get; } = [];
    public List<(Guid PlayerId, string NpcKey, string OfferingKey, Guid? SourceRunId)> ClaimedOfferings { get; } = [];
    public List<(Guid PlayerId, string NpcKey, string MilestoneKey, Guid? SourceRunId)> GrantedMilestones { get; } = [];
    public List<(Guid PlayerId, Guid CharacterId, string ItemKey)> EquippedItems { get; } = [];
    public List<(Guid PlayerId, Guid CharacterId, string ItemKey)> UnequippedItems { get; } = [];
    public List<(Guid PlayerId, IReadOnlyCollection<string> ItemDefinitionKeys, Guid? SourceRunId)> AddedPermanentItems { get; } = [];
    public List<(Guid PlayerId, string ItemDefinitionKey, string LiquidDefinitionKey)> SetPermanentItemContents { get; } = [];
    public List<(Guid PlayerId, string ItemDefinitionKey)> ClearedPermanentItemContents { get; } = [];
    public List<(Guid PlayerId, string CompanionDefinitionKey, string DisplayName, IReadOnlyCollection<string> SkillKeys)> RecruitedCompanions { get; } = [];

    public void SeedClaimedOffering(Guid playerId, string npcKey, string offeringKey)
        => _claimedOfferings.Add(Key(playerId, npcKey, offeringKey));

    public void SeedCurrencyBalance(Guid playerId, int amount) => _currencyBalances[playerId] = amount;

    public void SeedHimLitCurrencyBalance(Guid playerId, int amount) => _himLitCurrencyBalances[playerId] = amount;

    private static string Key(Guid playerId, string npcKey, string offeringKey) => $"{playerId}|{npcKey}|{offeringKey}";

    private int GetBalance(Guid playerId) => _currencyBalances.GetValueOrDefault(playerId);

    private int GetHimLitBalance(Guid playerId) => _himLitCurrencyBalances.GetValueOrDefault(playerId);

    public Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> EquipItemAsync(
        Guid playerId,
        Guid characterId,
        string itemKey,
        string slot,
        CancellationToken cancellationToken)
    {
        EquippedItems.Add((playerId, characterId, itemKey));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> UnequipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken)
    {
        UnequippedItems.Add((playerId, characterId, itemKey));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> AddPermanentItemsAsync(Guid playerId, IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        AddedPermanentItems.Add((playerId, itemDefinitionKeys, sourceRunId));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> SetPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, string liquidDefinitionKey, CancellationToken cancellationToken)
    {
        SetPermanentItemContents.Add((playerId, itemDefinitionKey, liquidDefinitionKey));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> ClearPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, CancellationToken cancellationToken)
    {
        ClearedPermanentItemContents.Add((playerId, itemDefinitionKey));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken, string source = "devtools")
    {
        UnlockedSkills.Add((playerId, characterId, skillKey, source));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<PlayerProfileView> AwardCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        AwardedCurrency.Add((playerId, amount));
        _currencyBalances[playerId] = GetBalance(playerId) + amount;
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<bool> TrySpendCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        var balance = GetBalance(playerId);
        var succeeded = balance >= amount;
        if (succeeded)
            _currencyBalances[playerId] = balance - amount;

        SpentCurrencyAttempts.Add((playerId, amount, succeeded));
        return Task.FromResult(succeeded);
    }

    public Task<PlayerProfileView> AwardHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        AwardedHimLitCurrency.Add((playerId, amount));
        _himLitCurrencyBalances[playerId] = GetHimLitBalance(playerId) + amount;
        return Task.FromResult(EmptyProfile(playerId));
    }

    public Task<bool> TrySpendHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        var balance = GetHimLitBalance(playerId);
        var succeeded = !ForceHimLitSpendFailure && balance >= amount;
        if (succeeded)
            _himLitCurrencyBalances[playerId] = balance - amount;

        SpentHimLitCurrencyAttempts.Add((playerId, amount, succeeded));
        return Task.FromResult(succeeded);
    }

    public Task<bool> HasClaimedNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, CancellationToken cancellationToken)
        => Task.FromResult(_claimedOfferings.Contains(Key(playerId, npcKey, offeringKey)));

    public Task ClaimNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        _claimedOfferings.Add(Key(playerId, npcKey, offeringKey));
        ClaimedOfferings.Add((playerId, npcKey, offeringKey, sourceRunId));
        return Task.CompletedTask;
    }

    public Task GrantReputationMilestoneAsync(Guid playerId, string npcKey, string milestoneKey, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        GrantedMilestones.Add((playerId, npcKey, milestoneKey, sourceRunId));
        return Task.CompletedTask;
    }

    public Task<PlayerProfileView> RecruitCompanionAsync(
        Guid playerId, string companionDefinitionKey, string displayName,
        int maxVitality, int attackPower, int defense, int startingGuard,
        int speed, int initiative, int focus, int mana, int charge,
        IReadOnlyCollection<string> skillKeys,
        CancellationToken cancellationToken,
        int magicAttack = 0, int magicDefense = 0)
    {
        RecruitedCompanions.Add((playerId, companionDefinitionKey, displayName, skillKeys));
        return Task.FromResult(EmptyProfile(playerId));
    }

    public List<(Guid PlayerId, IReadOnlyCollection<NpcReputationScoreView> Scores)> UpsertedNpcScores { get; } = [];
    public Dictionary<Guid, List<NpcReputationScoreView>> SeededNpcScores { get; } = new();

    public Task<IReadOnlyCollection<NpcReputationScoreView>> GetNpcReputationScoresAsync(Guid playerId, CancellationToken cancellationToken)
    {
        if (SeededNpcScores.TryGetValue(playerId, out var scores))
            return Task.FromResult<IReadOnlyCollection<NpcReputationScoreView>>(scores);
        return Task.FromResult<IReadOnlyCollection<NpcReputationScoreView>>([]);
    }

    public Task UpsertNpcReputationScoresAsync(Guid playerId, Guid sourceRunId, IReadOnlyCollection<NpcReputationScoreView> scores, CancellationToken cancellationToken)
    {
        UpsertedNpcScores.Add((playerId, scores));
        return Task.CompletedTask;
    }

    private PlayerProfileView EmptyProfile(Guid playerId) => new(
        playerId, "Stub Player", [], new PlayerProgressionView(GetBalance(playerId), GetHimLitBalance(playerId)));
}
