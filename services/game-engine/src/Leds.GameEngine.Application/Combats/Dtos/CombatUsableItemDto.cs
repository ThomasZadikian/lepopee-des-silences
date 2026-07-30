namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>
/// Représentation d'un objet consommable utilisable pendant un combat,
/// exposé dans <see cref="CombatRuntimeDto"/> pour que le frontend
/// l'affiche aux côtés des compétences.
/// </summary>
public sealed record CombatUsableItemDto(
    Guid ItemId,
    string DefinitionKey,
    string DisplayName,
    string EffectType,
    int EffectAmount,
    int Quantity,
    string TargetingType,
    int TacticalRange,
    string TacticalAreaShape,
    bool RequiresLineOfSight);
