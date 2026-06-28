namespace Leds.GameEngine.Application.Events.Dtos;

public sealed record NpcDialogueViewDto(
    string NpcKey,
    string Speaker,
    string NodeKey,
    IReadOnlyCollection<string> Lines,
    IReadOnlyCollection<NodeEventChoiceDto> Choices,
    string AggregateState,
    bool EncounterActive);