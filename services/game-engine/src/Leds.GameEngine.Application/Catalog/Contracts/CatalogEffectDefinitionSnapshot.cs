namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEffectDefinitionSnapshot(
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
