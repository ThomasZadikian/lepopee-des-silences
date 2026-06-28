namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogDialogueRequirement(
    string Kind,
    string? FlagKey,
    string? WoundKey,
    string? RequiredWoundState);