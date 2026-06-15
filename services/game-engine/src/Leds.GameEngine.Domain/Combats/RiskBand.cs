namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Qualitative band derived from the actual risk level of a combat node.
/// Mapping: 0–24 Low, 25–49 Moderate, 50–74 High, 75–100 Critical.
/// </summary>
public enum RiskBand
{
    Low = 0,
    Moderate = 1,
    High = 2,
    Critical = 3
}