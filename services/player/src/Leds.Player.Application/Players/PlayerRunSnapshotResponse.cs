namespace Leds.Player.Application.Players;

public sealed record PlayerRunSnapshotResponse(
    Guid PlayerId,
    string DisplayName,
    IReadOnlyCollection<PlayerRunSnapshotCharacterResponse> Characters);

public sealed record PlayerRunSnapshotCharacterResponse(
    Guid CharacterId,
    string DefinitionKey,
    string DisplayName,
    int MaxVitality,
    int BaseMana,
    int BaseCharge,
    IReadOnlyCollection<string> SkillKeys);
