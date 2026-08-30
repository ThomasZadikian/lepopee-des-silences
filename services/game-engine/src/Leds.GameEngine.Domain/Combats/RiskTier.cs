namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// The single, player-visible danger tier of a combat-flavored map node.
/// Replaces the old dual representation (a raw 0-100 roll rescaled through two
/// separate, non-reconciled formulas) with one source of truth used everywhere:
/// enemy eligibility/budget, stat scaling, and reward scaling.
/// Backed by int and numbered 1-5 so it slots into existing 1-5 comparisons
/// (e.g. <see cref="Leds.GameEngine.Application.Catalog.CatalogEnemyDefinition.MinRiskLevel"/>)
/// without rescaling.
/// </summary>
public enum RiskTier
{
    Calme = 1,
    Tendu = 2,
    Dangereux = 3,
    Perilleux = 4,
    Fatal = 5,
}
