using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerProfile
{
    private PlayerProfile(
        PlayerId id,
        string displayName,
        PlayerRoster roster,
        PlayerProgression progression,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        DisplayName = displayName;
        Roster = roster;
        Progression = progression;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public PlayerId Id { get; }
    public string DisplayName { get; }
    public PlayerRoster Roster { get; }
    public PlayerProgression Progression { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

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

    private void AddDefaultCharacter()
    {
        var defaultCharacter = PlayerCharacter.Create(
            definitionKey: "character.player.self",
            displayName: "Le Porteur",
            statBlock: PlayerCharacterStatBlock.CreateDefaultPorteur(),
            skills:
            [
                PlayerCharacterSkill.Create("skill.basic.strike", CreatedAtUtc, "default", isEquipped: true),
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
    /// Awards a permanent stat point. Profile-level (not character-scoped) —
    /// points aren't earned per-character.
    /// </summary>
    public void AwardStatPoint(DateTimeOffset now)
    {
        Progression.AwardStatPoint();
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
        DateTimeOffset updatedAtUtc)
    {
        return new PlayerProfile(id, displayName, roster, progression, createdAtUtc, updatedAtUtc);
    }
}
