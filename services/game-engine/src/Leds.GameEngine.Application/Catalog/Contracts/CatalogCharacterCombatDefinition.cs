namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogCharacterCombatDefinition(
    string DefinitionKey,
    string Kind,
    string CombatArchetypeCode,
    string EmotionalRegister);
