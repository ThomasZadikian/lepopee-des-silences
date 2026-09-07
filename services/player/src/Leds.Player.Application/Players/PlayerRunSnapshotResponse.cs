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
    IReadOnlyCollection<string> SkillKeys,
    PlayerRunSnapshotCharacterStatsResponse? Stats = null,
    IReadOnlyCollection<string>? EquippedItemKeys = null,
    IReadOnlyCollection<PlayerRunSnapshotEquipmentResponse>? EquipmentLoadout = null);

public sealed record PlayerRunSnapshotEquipmentResponse(
    Guid ItemInstanceId,
    string ItemDefinitionKey,
    string Position);

public sealed record PlayerRunSnapshotCharacterStatsResponse(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    int MagicAttack = 0,
    int MagicDefense = 0,
    int Movement = 4);
