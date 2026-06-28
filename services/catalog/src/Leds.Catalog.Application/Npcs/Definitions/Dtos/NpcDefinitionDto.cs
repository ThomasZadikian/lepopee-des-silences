using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    IReadOnlyCollection<string> CompatiblePalaceRoomStates,
    IReadOnlyCollection<string> CompatibleRoomClimates,
    int? MinDepth,
    int? MaxDepth,
    string EmotionalAffinity,
    bool IsRecurring,
    NpcPersonaDto? Persona,
    NpcDialogueGraphDto? DialogueGraph,
    IReadOnlyCollection<NpcWoundDto> Wounds,
    IReadOnlyCollection<string> EncounterKeys)
{
    public static NpcDefinitionDto FromDomain(INpcDefinition definition)
    {
        return new NpcDefinitionDto(
            definition.Id.Value,
            definition.Key.Value,
            definition.Name.Value,
            definition.Description.Value,
            definition.Version.Value,
            definition.Status.ToString(),
            definition.Tags.ToArray(),
            definition.CompatibleRoomTypes.ToArray(),
            definition.CompatiblePalaceRoomStates.ToArray(),
            definition.CompatibleRoomClimates.ToArray(),
            definition.MinDepth,
            definition.MaxDepth,
            definition.EmotionalAffinity.ToString(),
            definition.IsRecurring,
            definition.Persona is null ? null : NpcPersonaDto.FromDomain(definition.Persona),
            definition.DialogueGraph is null ? null : NpcDialogueGraphDto.FromDomain(definition.DialogueGraph),
            definition.Wounds.Select(NpcWoundDto.FromDomain).ToArray(),
            definition.EncounterKeys.ToArray());
    }
}