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
            maxVitality: 100,
            baseMana: 0,
            baseCharge: 0,
            skillKeys: ["skill.basic.strike", "skill.basic.guard"]);

        Roster.AddCharacter(defaultCharacter);
    }

    public void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
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
