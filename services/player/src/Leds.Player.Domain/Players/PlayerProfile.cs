using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerProfile
{
    private readonly List<PlayerPermanentUnlock> _permanentUnlocks;
    private readonly List<PlayerPermanentItem> _permanentItems;
    private readonly List<NpcReputationScore> _npcReputationScores;

    private PlayerProfile(PlayerProfileSnapshot snapshot)
    {
        Id = snapshot.Id;
        DisplayName = snapshot.DisplayName;
        Roster = snapshot.Roster;
        Progression = snapshot.Progression;
        MainStoryProgress = snapshot.MainStoryProgress
            ?? global::Leds.Player.Domain.Players.MainStoryProgress.CreateDefault();
        CreatedAtUtc = snapshot.CreatedAtUtc;
        UpdatedAtUtc = snapshot.UpdatedAtUtc;
        _permanentUnlocks = snapshot.PermanentUnlocks?.ToList() ?? [];
        _permanentItems = snapshot.PermanentItems?.ToList() ?? [];
        _npcReputationScores = snapshot.NpcReputationScores?.ToList() ?? [];
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

        return new PlayerProfile(new PlayerProfileSnapshot
        {
            Id = PlayerId.New(),
            DisplayName = displayName.Trim(),
            Roster = PlayerRoster.Create(),
            Progression = PlayerProgression.CreateDefault(),
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            MainStoryProgress = MainStoryProgress.CreateDefault()
        });
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

    public PlayerCharacter CreatePlayableCharacter(
        string displayName,
        ArchetypeDefinitionSnapshot archetype,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        archetype.Validate();

        var equipped = archetype.StarterEquippedSkills.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skills = archetype.StarterKnownSkills
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => PlayerCharacterSkill.Create(key, now, "archetype", equipped.Contains(key)))
            .ToArray();
        if (skills.Length == 0)
            throw new DomainException("An archetype must declare at least one starter skill.");

        var character = PlayerCharacter.CreatePlayable(
            "character.player.self", displayName, archetype.Key, archetype.BaseStats, skills);

        foreach (var starter in archetype.StarterEquipment)
        {
            if (string.IsNullOrWhiteSpace(starter.ItemDefinitionKey))
                throw new DomainException("Starter item definition key is required.");
            var id = OwnedItemInstanceId.New();
            var owned = PlayerPermanentItem.Create(id, starter.ItemDefinitionKey, null, now);
            _permanentItems.Add(owned);
            var item = PlayerCharacterItem.Rehydrate(id, starter.ItemDefinitionKey, now, "archetype", null);
            character.AddItem(item);
            character.EquipItem(id, starter.Position);
        }

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
            // Delivery retries for the same run remain idempotent, while a later run may
            // legitimately award another instance of the same definition (for two rings,
            // consumable containers, etc.).
            if (_permanentItems.Any(item => item.SourceRunId == sourceRunId
                && string.Equals(item.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase)))
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

    public void EquipItem(
        PlayerCharacterId characterId,
        OwnedItemInstanceId itemInstanceId,
        EquipmentPosition targetPosition,
        IReadOnlyCollection<EquipmentSlotKind> allowedSlots,
        DateTimeOffset now)
    {
        var owned = _permanentItems.FirstOrDefault(item => item.Id == itemInstanceId)
            ?? throw new DomainException($"Item instance '{itemInstanceId}' is not in the shared inventory.");
        if (!allowedSlots.Any(slot => EquipmentPositionCompatibility.Accepts(targetPosition, slot)))
            throw new DomainException(
                $"Item '{owned.ItemDefinitionKey}' cannot be equipped in position '{targetPosition}'.");

        var target = Roster.GetRequired(characterId);
        var attachedElsewhere = Roster.Characters.FirstOrDefault(character =>
            character.Id != characterId && character.Items.Any(item => item.Id == itemInstanceId));
        if (attachedElsewhere is not null)
            throw new DomainException($"Item instance '{itemInstanceId}' is already assigned to another character.");

        var characterItem = target.Items.FirstOrDefault(item => item.Id == itemInstanceId);
        if (characterItem is null)
        {
            characterItem = PlayerCharacterItem.Rehydrate(
                owned.Id, owned.ItemDefinitionKey, owned.AcquiredAtUtc, "shared-inventory", null);
            target.AddItem(characterItem);
        }

        target.EquipItem(itemInstanceId, targetPosition);
        Touch(now);
    }

    public void UnequipItem(
        PlayerCharacterId characterId,
        OwnedItemInstanceId itemInstanceId,
        DateTimeOffset now)
    {
        var character = Roster.GetRequired(characterId);
        character.UnequipItem(itemInstanceId);
        character.DetachItem(itemInstanceId);
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

    public void AdvanceMainStory(MainStoryAdvance advance)
    {
        ArgumentNullException.ThrowIfNull(advance);
        if (MainStoryProgress.IsCompleted)
            return;

        MainStoryProgress.Advance(
            advance.SequenceKey,
            advance.SequenceVersion,
            advance.StepKey,
            advance.CheckpointKey);
        foreach (var roomKey in advance.UnlockedRoomKeys)
            MainStoryProgress.UnlockRoom(roomKey);
        foreach (var roomKey in advance.VisibleRoomKeys)
            MainStoryProgress.RevealRoom(roomKey);
        if (advance.Complete)
            MainStoryProgress.Complete();
        Touch(advance.Now);
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
        DateTimeOffset updatedAtUtc)
    {
        return Rehydrate(new PlayerProfileSnapshot
        {
            Id = id,
            DisplayName = displayName,
            Roster = roster,
            Progression = progression,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        });
    }

    public static PlayerProfile Rehydrate(PlayerProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new PlayerProfile(snapshot);
    }
}

public sealed record MainStoryAdvance
{
    public required string SequenceKey { get; init; }
    public required string SequenceVersion { get; init; }
    public required string StepKey { get; init; }
    public string? CheckpointKey { get; init; }
    public required IReadOnlyCollection<string> UnlockedRoomKeys { get; init; }
    public required IReadOnlyCollection<string> VisibleRoomKeys { get; init; }
    public required bool Complete { get; init; }
    public required DateTimeOffset Now { get; init; }
}

public sealed record PlayerProfileSnapshot
{
    public required PlayerId Id { get; init; }
    public required string DisplayName { get; init; }
    public required PlayerRoster Roster { get; init; }
    public required PlayerProgression Progression { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }
    public MainStoryProgress? MainStoryProgress { get; init; }
    public IReadOnlyCollection<PlayerPermanentUnlock>? PermanentUnlocks { get; init; }
    public IReadOnlyCollection<PlayerPermanentItem>? PermanentItems { get; init; }
    public IReadOnlyCollection<NpcReputationScore>? NpcReputationScores { get; init; }
}
