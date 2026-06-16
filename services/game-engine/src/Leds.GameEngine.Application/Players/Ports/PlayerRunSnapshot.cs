namespace Leds.GameEngine.Application.Players.Ports;

public sealed record PlayerRunSnapshot(
    Guid PlayerId,
    string DisplayName,
    IReadOnlyCollection<PlayerRunSnapshotCharacter> Characters);

public sealed record PlayerRunSnapshotCharacter(
    Guid CharacterId,
    string DefinitionKey,
    string DisplayName,
    PlayerRunSnapshotCharacterStats Stats,
    IReadOnlyCollection<PlayerRunSnapshotCharacterSkill> Skills)
{
    public int MaxVitality => Stats.MaxVitality;
    public int BaseMana => Stats.Mana;
    public int BaseCharge => Stats.Charge;
    public IReadOnlyCollection<string> SkillKeys => Skills.Select(s => s.SkillDefinitionKey).ToArray();
}

public sealed record PlayerRunSnapshotCharacterStats(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge);

public sealed record PlayerRunSnapshotCharacterSkill(
    string SkillDefinitionKey,
    string DisplayName,
    string SkillType,
    string TargetingMode,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower);
