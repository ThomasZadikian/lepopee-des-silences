namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Versions required to replay a run with the same authored and generated contracts.
/// The current runtime already persists these values separately; this value object makes
/// the contract explicit without inventing a Catalog-wide version that does not exist yet.
/// </summary>
public sealed record ContentVersionSet(
    string GeneratorVersion,
    string MarkovMatrixVersion,
    string EmotionalAffinityMatrixVersion);
