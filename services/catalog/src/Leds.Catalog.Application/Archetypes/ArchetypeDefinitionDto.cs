namespace Leds.Catalog.Application.Archetypes;

public sealed record ArchetypeDefinitionDto(
    string Key,
    string DisplayName,
    string Description,
    ArchetypeBaseStatsDto BaseStats,
    IReadOnlyCollection<string> ProficiencyTags,
    IReadOnlyCollection<StarterEquipmentDto> StarterEquipment,
    IReadOnlyCollection<string> StarterKnownSkills,
    IReadOnlyCollection<string> StarterEquippedSkills);

public sealed record ArchetypeBaseStatsDto(
    int MaxVitality,
    int AttackPower,
    int MagicAttack,
    int Defense,
    int MagicDefense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    int Movement);

public sealed record StarterEquipmentDto(string ItemDefinitionKey, string EquipmentPosition);
