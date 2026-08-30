using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerProfile
{
    private readonly List<PlayerPermanentUnlock> _permanentUnlocks;
    private readonly List<PlayerPermanentItem> _permanentItems;
    private readonly List<NpcReputationScore> _npcReputationScores;

    private PlayerProfile(
        PlayerId id,
        string displayName,
        PlayerRoster roster,
        PlayerProgression progression,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        MainStoryProgress? mainStoryProgress = null,
        IReadOnlyCollection<PlayerPermanentUnlock>? permanentUnlocks = null,
        IReadOnlyCollection<PlayerPermanentItem>? permanentItems = null,
        IReadOnlyCollection<NpcReputationScore>? npcReputationScores = null)
    {
        Id = id;
        DisplayName = displayName;
        Roster = roster;
        Progression = progression;
        MainStoryProgress = mainStoryProgress
            ?? global::Leds.Player.Domain.Players.MainStoryProgress.CreateDefault();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _permanentUnlocks = permanentUnlocks?.ToList() ?? [];
        _permanentItems = permanentItems?.ToList() ?? [];
        _npcReputationScores = npcReputationScores?.ToList() ?? [];
    }

    public PlayerId Id { get; }
    public string DisplayName { get; private set; }
    public PlayerRoster Roster { get; }
    public PlayerProgression Progression { get; }
    public MainStoryProgress MainStoryProgress { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<PlayerPermanentUnlock> PermanentUnlocks => _permanentUnlocks.AsReadOnly();
    public IReadOnlyCollection<PlayerPermanentItem> PermanentItems => _permanentItems.AsReadOnly();
    public IReadOnlyCollection<NpcReputationScore> NpcReputationScores => _npcReputationScores.AsReadOnly();

    public static PlayerProfile Create(string displayName, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player display name is required.");

        return new PlayerProfile(
            PlayerId.New(),
            displayName.Trim(),
            PlayerRoster.Create(),
            PlayerProgression.CreateDefault(),
            createdAtUtc,
            createdAtUtc,
            MainStoryProgress.CreateDefault());
    }

    public PlayerCharacter CreatePlayableCharacter(
        string displayName,
        string archetypeKey,
        DateTimeOffset now)
    {
        var character = PlayerCharacter.CreatePlayable(
            "character.player.self",
            displayName,
            archetypeKey,
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [PlayerCharacterSkill.Create("skill.basic.guard", now, "archetype", isEquipped: true)]);

        Roster.AddCharacter(character);
        Touch(now);
        return character;
    }

    /// <summary>
    /// Irreversibly replaces the account-facing alias with an anonymous identifier while
    /// retaining non-identifying progression required for referential and gameplay integrity.
    /// Identity credentials and other PII are anonymized by their own bounded objects.
    /// </summary>
    public void Anonymize(string anonymousDisplayName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(anonymousDisplayName))
            throw new DomainException("Anonymous display name is required.");

        DisplayName = anonymousDisplayName.Trim();
        Touch(now);
    }

    public bool HasPermanentUnlock(string unlockKey) =>
        _permanentUnlocks.Any(u => string.Equals(u.UnlockKey, unlockKey, StringComparison.OrdinalIgnoreCase));

    public void GrantPermanentUnlock(string unlockKey, string unlockType, Guid? sourceRunId, DateTimeOffset now)
    {
        if (HasPermanentUnlock(unlockKey))
            return;

        _permanentUnlocks.Add(PlayerPermanentUnlock.Create(unlockKey, unlockType, sourceRunId, now));
        Touch(now);
    }

    public bool HasPermanentItem(string itemDefinitionKey) =>
        _permanentItems.Any(i => string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase));

    public void AddPermanentItems(IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, DateTimeOffset now)
    {
        foreach (var itemDefinitionKey in itemDefinitionKeys)
        {
            if (HasPermanentItem(itemDefinitionKey))
                continue;

            _permanentItems.Add(PlayerPermanentItem.Create(itemDefinitionKey, sourceRunId, now));
        }

        Touch(now);
    }

    public void SetPermanentItemContent(string itemDefinitionKey, string liquidDefinitionKey, DateTimeOffset now)
    {
        var item = _permanentItems.FirstOrDefault(i =>
            string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException($"Item '{itemDefinitionKey}' is not in the permanent backpack.");

        item.SetContainedLiquid(liquidDefinitionKey);
        Touch(now);
    }

    public void ClearPermanentItemContent(string itemDefinitionKey, DateTimeOffset now)
    {
        var item = _permanentItems.FirstOrDefault(i =>
            string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException($"Item '{itemDefinitionKey}' is not in the permanent backpack.");

        item.ClearContainedLiquid();
        Touch(now);
    }

    public void EquipItem(
        PlayerCharacterId characterId,
        string itemKey,
        EquipmentSlotKind slot,
        DateTimeOffset now)
    {
        if (!HasPermanentItem(itemKey))
            throw new DomainException($"Item '{itemKey}' is not in the permanent backpack.");

        var character = Roster.GetRequired(characterId);
        character.AddItem(PlayerCharacterItem.Create(itemKey, now, slot: slot));
        character.EquipItem(itemKey, slot);
        Touch(now);
    }

    public void EquipItem(PlayerCharacterId characterId, string itemKey, DateTimeOffset now)
        => EquipItem(characterId, itemKey, EquipmentSlotKind.Relic, now);

    public void UnequipItem(PlayerCharacterId characterId, string itemKey, DateTimeOffset now)
    {
        Roster.GetRequired(characterId).UnequipItem(itemKey);
        Touch(now);
    }

    public void RecruitCompanion(
        string companionDefinitionKey,
        string displayName,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<string> skillKeys,
        DateTimeOffset now)
    {
        if (Roster.Characters.Any(c =>
                string.Equals(c.DefinitionKey, companionDefinitionKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(c.CharacterType, "Companion", StringComparison.OrdinalIgnoreCase)))
            return;

        var skills = skillKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => PlayerCharacterSkill.Create(key, now, "npc-offering", isEquipped: true))
            .ToArray();

        var companion = PlayerCharacter.Create(
            companionDefinitionKey,
            displayName,
            statBlock,
            skills,
            characterType: "Companion");

        Roster.AddCharacter(companion);
        Touch(now);
    }

    public void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AdvanceMainStory(
        string sequenceKey,
        string sequenceVersion,
        string stepKey,
        string? checkpointKey,
        IReadOnlyCollection<string> unlockedRoomKeys,
        IReadOnlyCollection<string> visibleRoomKeys,
        bool complete,
        DateTimeOffset now)
    {
        if (MainStoryProgress.IsCompleted)
            return;

        MainStoryProgress.Advance(sequenceKey, sequenceVersion, stepKey, checkpointKey);
        foreach (var roomKey in unlockedRoomKeys)
            MainStoryProgress.UnlockRoom(roomKey);
        foreach (var roomKey in visibleRoomKeys)
            MainStoryProgress.RevealRoom(roomKey);
        if (complete)
            MainStoryProgress.Complete();
        Touch(now);
    }

    public void UnlockDifficultyLevel(int level, DateTimeOffset now)
    {
        if (MainStoryProgress.UnlockNextDifficulty(level))
            Touch(now);
    }

    public void UpsertNpcReputationScores(IReadOnlyCollection<NpcReputationScore> scores, DateTimeOffset now)
    {
        foreach (var score in scores)
        {
            var existing = _npcReputationScores.FirstOrDefault(s =>
                string.Equals(s.NpcKey, score.NpcKey, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                _npcReputationScores.Add(score);
            }
            else
            {
                existing.ApplyDelta(
                    score.Score - existing.Score,
                    score.TimesMet - existing.TimesMet,
                    score.CurrentDialogueNodeKey,
                    now);
            }
        }

        Touch(now);
    }

    public NpcReputationScore? GetNpcReputationScore(string npcKey) =>
        _npcReputationScores.FirstOrDefault(s =>
            string.Equals(s.NpcKey, npcKey, StringComparison.OrdinalIgnoreCase));

    public void LearnSkill(PlayerCharacterId characterId, string skillKey, string? source, DateTimeOffset now)
    {
        Roster.GetRequired(characterId).LearnSkill(PlayerCharacterSkill.Create(skillKey, now, source));
        Touch(now);
    }

    public void EquipSkill(PlayerCharacterId characterId, string skillKey, DateTimeOffset now)
    {
        Roster.GetRequired(characterId).EquipSkill(skillKey);
        Touch(now);
    }

    public void UnequipSkill(PlayerCharacterId characterId, string skillKey, DateTimeOffset now)
    {
        Roster.GetRequired(characterId).UnequipSkill(skillKey);
        Touch(now);
    }

    public void AwardCurrency(DateTimeOffset now, int amount)
    {
        Progression.AwardCurrency(amount);
        Touch(now);
    }

    public bool TrySpendCurrency(DateTimeOffset now, int amount)
    {
        var succeeded = Progression.TrySpendCurrency(amount);
        if (succeeded)
            Touch(now);

        return succeeded;
    }

    public void AwardHimLitCurrency(DateTimeOffset now, int amount)
    {
        Progression.AwardHimLitCurrency(amount);
        Touch(now);
    }

    public bool TrySpendHimLitCurrency(DateTimeOffset now, int amount)
    {
        var succeeded = Progression.TrySpendHimLitCurrency(amount);
        if (succeeded)
            Touch(now);

        return succeeded;
    }

    public static PlayerProfile Rehydrate(
        PlayerId id,
        string displayName,
        PlayerRoster roster,
        PlayerProgression progression,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        MainStoryProgress? mainStoryProgress = null,
        IReadOnlyCollection<PlayerPermanentUnlock>? permanentUnlocks = null,
        IReadOnlyCollection<PlayerPermanentItem>? permanentItems = null,
        IReadOnlyCollection<NpcReputationScore>? npcReputationScores = null)
    {
        return new PlayerProfile(
            id,
            displayName,
            roster,
            progression,
            createdAtUtc,
            updatedAtUtc,
            mainStoryProgress,
            permanentUnlocks,
            permanentItems,
            npcReputationScores);
    }
}
