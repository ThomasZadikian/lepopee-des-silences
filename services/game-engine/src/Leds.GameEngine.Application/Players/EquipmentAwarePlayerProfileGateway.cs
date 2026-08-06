using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Decorates <see cref="IPlayerProfileGateway"/> so every <see cref="PlayerProfileView"/>
/// it returns carries EFFECTIVE stats (base stats + equipped items' <c>StatBonus</c>/
/// <c>StatBonusPercent</c> catalog effects) instead of the player-service's raw base
/// stat block.
/// </summary>
/// <remarks>
/// Player-service (the system of record for a character's stat block) has no notion of
/// the Catalog's item-authored stat bonuses — it only knows which items are equipped.
/// Combat already resolves the very same bonuses at run start/resync via
/// <see cref="PlayerStatMerger"/> (see <c>StartRunCommandHandler</c> and
/// <c>SyncPartyStatsCommandHandler</c>). Without this decorator, the permanent
/// character sheet ("Équipe" → onglet Statistiques) showed base stats only — equipping
/// gear never visibly changed anything there, even though combat correctly used the
/// boosted stats. Wrapping the gateway (rather than patching each handler) guarantees
/// every current and future caller — GetProfile, equip/unequip item or skill, spend
/// stat point, etc. — sees the same effective stats, with no risk of one call path
/// drifting back to raw base stats.
/// </remarks>
public sealed class EquipmentAwarePlayerProfileGateway : IPlayerProfileGateway
{
    private readonly IPlayerProfileGateway _inner;
    private readonly PlayerSkillMerger _skillMerger;
    private readonly PlayerStatMerger _statMerger;

    public EquipmentAwarePlayerProfileGateway(
        IPlayerProfileGateway inner,
        PlayerSkillMerger skillMerger,
        PlayerStatMerger statMerger)
    {
        _inner = inner;
        _skillMerger = skillMerger;
        _statMerger = statMerger;
    }

    public Task AwardStatPointAsync(Guid playerId, CancellationToken cancellationToken)
        => _inner.AwardStatPointAsync(playerId, cancellationToken);

    public async Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.GetProfileAsync(playerId, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.EquipSkillAsync(playerId, characterId, skillKey, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.UnequipSkillAsync(playerId, characterId, skillKey, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> EquipItemAsync(
        Guid playerId, Guid characterId, string itemKey, string slot, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.EquipItemAsync(playerId, characterId, itemKey, slot, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> UnequipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.UnequipItemAsync(playerId, characterId, itemKey, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> AddPermanentItemsAsync(
        Guid playerId, IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.AddPermanentItemsAsync(playerId, itemDefinitionKeys, sourceRunId, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> SetPermanentItemContentAsync(
        Guid playerId, string itemDefinitionKey, string liquidDefinitionKey, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.SetPermanentItemContentAsync(playerId, itemDefinitionKey, liquidDefinitionKey, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> ClearPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.ClearPermanentItemContentAsync(playerId, itemDefinitionKey, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.SpendStatPointAsync(playerId, characterId, stat, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> UnlockSkillAsync(
        Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken, string source = "devtools")
        => await EnrichAsync(await _inner.UnlockSkillAsync(playerId, characterId, skillKey, cancellationToken, source), cancellationToken);

    public async Task<PlayerProfileView> AwardStatPointsAsync(Guid playerId, int amount, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.AwardStatPointsAsync(playerId, amount, cancellationToken), cancellationToken);

    public async Task<PlayerProfileView> AwardCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.AwardCurrencyAsync(playerId, amount, cancellationToken), cancellationToken);

    public Task<bool> TrySpendCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
        => _inner.TrySpendCurrencyAsync(playerId, amount, cancellationToken);

    public async Task<PlayerProfileView> AwardHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
        => await EnrichAsync(await _inner.AwardHimLitCurrencyAsync(playerId, amount, cancellationToken), cancellationToken);

    public Task<bool> TrySpendHimLitCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
        => _inner.TrySpendHimLitCurrencyAsync(playerId, amount, cancellationToken);

    public Task<bool> HasClaimedNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, CancellationToken cancellationToken)
        => _inner.HasClaimedNpcOfferingAsync(playerId, npcKey, offeringKey, cancellationToken);

    public Task ClaimNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, Guid? sourceRunId, CancellationToken cancellationToken)
        => _inner.ClaimNpcOfferingAsync(playerId, npcKey, offeringKey, sourceRunId, cancellationToken);

    public Task GrantReputationMilestoneAsync(Guid playerId, string npcKey, string milestoneKey, Guid? sourceRunId, CancellationToken cancellationToken)
        => _inner.GrantReputationMilestoneAsync(playerId, npcKey, milestoneKey, sourceRunId, cancellationToken);

    public async Task<PlayerProfileView> RecruitCompanionAsync(
        Guid playerId, string companionDefinitionKey, string displayName,
        int maxVitality, int attackPower, int defense, int startingGuard,
        int speed, int initiative, int focus, int mana, int charge,
        IReadOnlyCollection<string> skillKeys,
        CancellationToken cancellationToken,
        int magicAttack = 0, int magicDefense = 0)
        => await EnrichAsync(
            await _inner.RecruitCompanionAsync(
                playerId, companionDefinitionKey, displayName, maxVitality, attackPower, defense,
                startingGuard, speed, initiative, focus, mana, charge, skillKeys,
                cancellationToken,
                magicAttack, magicDefense),
            cancellationToken);

    public Task<IReadOnlyCollection<NpcReputationScoreView>> GetNpcReputationScoresAsync(Guid playerId, CancellationToken cancellationToken)
        => _inner.GetNpcReputationScoresAsync(playerId, cancellationToken);

    public Task UpsertNpcReputationScoresAsync(
        Guid playerId, Guid sourceRunId, IReadOnlyCollection<NpcReputationScoreView> scores, CancellationToken cancellationToken)
        => _inner.UpsertNpcReputationScoresAsync(playerId, sourceRunId, scores, cancellationToken);

    /// <summary>
    /// Recomputes each character's Stats as base + equipped-item bonuses, exactly the
    /// way <see cref="PlayerStatMerger"/> does for combat. Characters with no equipped
    /// items, or whose equipped items grant no StatBonus/StatBonusPercent effect, are
    /// returned unchanged (same object, no allocation).
    /// </summary>
    private async Task<PlayerProfileView> EnrichAsync(PlayerProfileView profile, CancellationToken cancellationToken)
    {
        var enrichedCharacters = new List<PlayerCharacterView>(profile.Characters.Count);
        var changed = false;

        foreach (var character in profile.Characters)
        {
            var equippedItemKeys = (character.Items ?? [])
                .Where(item => item.IsEquipped)
                .Select(item => item.ItemKey)
                .ToArray();

            if (equippedItemKeys.Length == 0)
            {
                enrichedCharacters.Add(character);
                continue;
            }

            var equipmentEffects = await _skillMerger.CollectEquippedItemEffectsAsync(equippedItemKeys, cancellationToken);
            if (equipmentEffects.Count == 0)
            {
                enrichedCharacters.Add(character);
                continue;
            }

            var baseStats = character.Stats;
            var runSnapshotStats = new PlayerRunSnapshotCharacterStats(
                baseStats.MaxVitality,
                baseStats.AttackPower,
                baseStats.Defense,
                baseStats.StartingGuard,
                baseStats.Speed,
                baseStats.Initiative,
                baseStats.Focus,
                baseStats.Mana,
                baseStats.Charge,
                baseStats.MagicAttack,
                baseStats.MagicDefense,
                baseStats.Movement);

            var effective = _statMerger.ComputeEffectiveStats(runSnapshotStats, equipmentEffects);

            var effectiveStats = baseStats with
            {
                MaxVitality = effective.MaxVitality,
                AttackPower = effective.AttackPower,
                Defense = effective.Defense,
                Speed = effective.Speed,
                Focus = effective.Focus,
                MagicAttack = effective.MagicAttack,
                MagicDefense = effective.MagicDefense,
                Mana = effective.Mana,
                Movement = effective.Movement,
            };

            enrichedCharacters.Add(character with { Stats = effectiveStats });
            changed = true;
        }

        return changed ? profile with { Characters = enrichedCharacters } : profile;
    }
}
