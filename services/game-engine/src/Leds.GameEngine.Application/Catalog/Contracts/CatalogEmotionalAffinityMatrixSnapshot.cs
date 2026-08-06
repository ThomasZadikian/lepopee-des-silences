namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEmotionalAffinityRuleSnapshot(
    string AttackingRegister,
    string DefendingRegister,
    string Outcome);

public sealed record CatalogEmotionalAffinityMatrixSnapshot(
    string Version,
    IReadOnlyCollection<CatalogEmotionalAffinityRuleSnapshot> Rules);
