using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed record ArchetypeDefinitionSnapshot(
    string Key,
    PlayerCharacterStatBlock BaseStats,
    IReadOnlyCollection<string> ProficiencyTags,
    IReadOnlyCollection<ArchetypeStarterEquipment> StarterEquipment,
    IReadOnlyCollection<string> StarterKnownSkills,
    IReadOnlyCollection<string> StarterEquippedSkills)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key)) throw new DomainException("Archetype key is required.");
        ArgumentNullException.ThrowIfNull(BaseStats);
        if (StarterEquippedSkills.Count > PlayerCharacter.MaxEquippedSkills)
            throw new DomainException($"An archetype cannot equip more than {PlayerCharacter.MaxEquippedSkills} starter skills.");
        if (StarterEquipment.Select(item => item.Position).Distinct().Count() != StarterEquipment.Count)
            throw new DomainException("Archetype starter equipment positions must be unique.");
        if (StarterEquippedSkills.Any(key => !StarterKnownSkills.Contains(key, StringComparer.OrdinalIgnoreCase)))
            throw new DomainException("Every equipped starter skill must also be known.");
    }
}

public sealed record ArchetypeStarterEquipment(string ItemDefinitionKey, EquipmentPosition Position);
