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

    public List<(Guid PlayerId, Guid CharacterId, string SkillKey, string Source)> UnlockedSkills { get; } = [];
    public List<(Guid PlayerId, int Amount)> AwardedStatPoints { get; } = [];
    public List<(Guid PlayerId, string NpcKey, string OfferingKey, Guid? SourceRunId)> ClaimedOfferings { get; } = [];
    public List<(Guid PlayerId, string NpcKey, string MilestoneKey, Guid? SourceRunId)> GrantedMilestones { get; } = [];
    public List<(Guid PlayerId, Guid CharacterId, string ItemKey)> EquippedItems { get; } = [];
    public List<(Guid PlayerId, Guid CharacterId, string ItemKey)> UnequippedItems { get; } = [];
    public List<(Guid PlayerId, IReadOnlyCollection<string> ItemDefinitionKeys, Guid? SourceRunId)> AddedPermanentItems { get; } = [];
    public List<(Guid PlayerId, string ItemDefinitionKey, string LiquidDefinitionKey)> SetPermanentItemContents { get; } = [];
    public List<(Guid PlayerId, string ItemDefinitionKey)> ClearedPermanentItemContents { get; } = [];

    public void SeedClaimedOffering(Guid playerId, string npcKey, string offeringKey)
        => _claimedOfferings.Add(Key(playerId, npcKey, offeringKey));

    private static string Key(Guid playerId, string npcKey, string offeringKey) => $"{playerId}|{npcKey}|{offeringKey}";

    public Task AwardStatPointAsync(Guid playerId, CancellationToken cancellationToken)
    {
        AwardedStatPoints.Add((playerId, 1));
        return Task.CompletedTask;
    }

    public Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken)
        => Task.FromResult(EmptyProfile(playerId));

    public Task<PlayerProfileView> EquipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken)
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

    public Task<PlayerProfileView> AwardStatPointsAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        AwardedStatPoints.Add((playerId, amount));
        return Task.FromResult(EmptyProfile(playerId));
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

    private static PlayerProfileView EmptyProfile(Guid playerId) => new(
        playerId, "Stub Player", [], new PlayerProgressionView(0, 0));
}
