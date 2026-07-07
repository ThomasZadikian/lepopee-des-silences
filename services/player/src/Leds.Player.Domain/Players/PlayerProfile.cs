using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerProfile
{
    private readonly List<PlayerPermanentUnlock> _permanentUnlocks;
    private readonly List<PlayerPermanentItem> _permanentItems;

    private PlayerProfile(
        PlayerId id,
        string displayName,
        PlayerRoster roster,
        PlayerProgression progression,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyCollection<PlayerPermanentUnlock>? permanentUnlocks = null,
        IReadOnlyCollection<PlayerPermanentItem>? permanentItems = null)
    {
        Id = id;
        DisplayName = displayName;
        Roster = roster;
        Progression = progression;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _permanentUnlocks = permanentUnlocks?.ToList() ?? [];
        _permanentItems = permanentItems?.ToList() ?? [];
    }

    public PlayerId Id { get; }
    public string DisplayName { get; }
    public PlayerRoster Roster { get; }
    public PlayerProgression Progression { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<PlayerPermanentUnlock> PermanentUnlocks => _permanentUnlocks.AsReadOnly();
    public IReadOnlyCollection<PlayerPermanentItem> PermanentItems => _permanentItems.AsReadOnly();

    public static PlayerProfile Create(string displayName, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player display name is required.");

        var profile = new PlayerProfile(
            PlayerId.New(),
            displayName.Trim(),
            PlayerRoster.Create(),
            PlayerProgression.CreateDefault(),
            createdAtUtc,
            createdAtUtc);

        profile.AddDefaultCharacter();

        return profile;
    }

    public bool HasPermanentUnlock(string unlockKey) =>
        _permanentUnlocks.Any(u => string.Equals(u.UnlockKey, unlockKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Grants a permanent, lifetime unlock (an NPC offering claimed, or a reputation
    /// milestone reached). No-op if already granted — permanent unlocks are never
    /// revoked or re-granted (decision: "jamais redonné, pour toute la vie du joueur").
    /// </summary>
    public void GrantPermanentUnlock(string unlockKey, string unlockType, Guid? sourceRunId, DateTimeOffset now)
    {
        if (HasPermanentUnlock(unlockKey))
            return;

        _permanentUnlocks.Add(PlayerPermanentUnlock.Create(unlockKey, unlockType, sourceRunId, now));
        Touch(now);
    }

    public bool HasPermanentItem(string itemDefinitionKey) =>
        _permanentItems.Any(i => string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Adds items to the permanent backpack (post-run selection confirmation). No-op per
    /// already-owned key — idempotent against command retries, mirrors GrantPermanentUnlock.
    /// </summary>
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

    /// <summary>
    /// Pours a liquid into an owned permanent container item (SFD container/liquid extension).
    /// Whether <paramref name="itemDefinitionKey"/> is actually a container is a catalog fact —
    /// the caller (application layer) must validate that before calling.
    /// </summary>
    public void SetPermanentItemContent(string itemDefinitionKey, string liquidDefinitionKey, DateTimeOffset now)
    {
        var item = _permanentItems.FirstOrDefault(i =>
            string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException($"Item '{itemDefinitionKey}' is not in the permanent backpack.");

        item.SetContainedLiquid(liquidDefinitionKey);
        Touch(now);
    }

    /// <summary>
    /// Empties an owned permanent container item, freeing it to receive a different liquid.
    /// </summary>
    public void ClearPermanentItemContent(string itemDefinitionKey, DateTimeOffset now)
    {
        var item = _permanentItems.FirstOrDefault(i =>
            string.Equals(i.ItemDefinitionKey, itemDefinitionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException($"Item '{itemDefinitionKey}' is not in the permanent backpack.");

        item.ClearContainedLiquid();
        Touch(now);
    }

    /// <summary>
    /// Equips a permanent-backpack item on a character. The item must already be owned
    /// (present in the permanent backpack) — equipping isn't how an item is acquired.
    /// </summary>
    public void EquipItem(PlayerCharacterId characterId, string itemKey, DateTimeOffset now)
    {
        if (!HasPermanentItem(itemKey))
            throw new DomainException($"Item '{itemKey}' is not in the permanent backpack.");

        var character = Roster.GetRequired(characterId);
        character.AddItem(PlayerCharacterItem.Create(itemKey, now));
        character.EquipItem(itemKey);
        Touch(now);
    }

    public void UnequipItem(PlayerCharacterId characterId, string itemKey, DateTimeOffset now)
    {
        Roster.GetRequired(characterId).UnequipItem(itemKey);
        Touch(now);
    }

    private void AddDefaultCharacter()
    {
        // skill.basic.strike is NOT listed here: it's the universal basic
        // attack (PlayerCharacter.BasicSkillKey), always usable regardless
        // of loadout, so it isn't part of the learnable/equippable pool.
        var defaultCharacter = PlayerCharacter.Create(
            definitionKey: "character.player.self",
            displayName: "Le Porteur",
            statBlock: PlayerCharacterStatBlock.CreateDefaultPorteur(),
            skills:
            [
                PlayerCharacterSkill.Create("skill.basic.guard", CreatedAtUtc, "default", isEquipped: true)
            ]);

        Roster.AddCharacter(defaultCharacter);
    }

    public void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }

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

    /// <summary>
    /// Awards permanent stat points. Profile-level (not character-scoped) —
    /// points aren't earned per-character.
    /// </summary>
    public void AwardStatPoint(DateTimeOffset now, int amount = 1)
    {
        Progression.AwardStatPoint(amount);
        Touch(now);
    }

    public void SpendStatPoint(PlayerCharacterId characterId, PlayerStatKind kind, DateTimeOffset now)
    {
        if (Progression.UnspentStatPoints <= 0)
            throw new DomainException("No stat points available to spend.");

        // Resolve the character before decrementing so an invalid characterId
        // never burns a point.
        var character = Roster.GetRequired(characterId);

        character.ApplyStatIncrement(kind);
        Progression.SpendStatPoint();
        Touch(now);
    }

    /// <summary>
    /// Rehydrates a player profile from a trusted persistence snapshot.
    /// This method must not be used to create a new player profile.
    /// </summary>
    public static PlayerProfile Rehydrate(
        PlayerId id,
        string displayName,
        PlayerRoster roster,
        PlayerProgression progression,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyCollection<PlayerPermanentUnlock>? permanentUnlocks = null,
        IReadOnlyCollection<PlayerPermanentItem>? permanentItems = null)
    {
        return new PlayerProfile(id, displayName, roster, progression, createdAtUtc, updatedAtUtc, permanentUnlocks, permanentItems);
    }
}
