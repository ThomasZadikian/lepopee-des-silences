namespace Leds.Player.Application.Abstractions;

public interface IEquipmentDefinitionGateway
{
    Task<EquipmentDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken);
}

public sealed record EquipmentDefinitionSnapshot(
    string Key,
    string DisplayName,
    IReadOnlyCollection<string> AllowedSlots,
    string? UniqueEquipGroup,
    IReadOnlyCollection<string> ProficiencyTags,
    IReadOnlyCollection<EquipmentEffectSnapshot> EquipmentEffects);

public sealed record EquipmentEffectSnapshot(string Kind, string? StatKind, int? Amount, string? SkillKey);
