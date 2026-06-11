namespace Leds.GameEngine.Application.Players.Ports;

public sealed record PlayerRunSnapshot(
    Guid PlayerId,
    string DisplayName,
    IReadOnlyCollection<PlayerRunSnapshotCharacter> Characters);

public sealed record PlayerRunSnapshotCharacter(
    Guid CharacterId,
    string DefinitionKey,
    string DisplayName,
    int MaxVitality,
    int BaseMana,
    int BaseCharge,
    IReadOnlyCollection<string> SkillKeys);
