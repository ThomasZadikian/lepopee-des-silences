namespace Leds.Catalog.Application.EffectSets.Dtos;

public sealed record EffectSetDto(
    Guid Id,
    string Key,
    string Version,
    string Status,
    IReadOnlyCollection<EffectDefinitionDto> Effects);

public sealed record EffectDefinitionDto(
    string EffectType,
    string TargetScope,
    decimal Value,
    string ValueMode,
    string Duration,
    string StackPolicy,
    string? Condition,
    int Order,
    string? BehaviorTag,
    string? GenerationTag,
    string? SelectionGroup);
