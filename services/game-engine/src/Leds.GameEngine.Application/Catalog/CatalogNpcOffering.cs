namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcOffering(
    string Key,
    string Kind,
    string? TargetKey,
    int Amount,
    bool IsMajor,
    IReadOnlyCollection<CatalogDialogueRequirement> UnlockConditions);
