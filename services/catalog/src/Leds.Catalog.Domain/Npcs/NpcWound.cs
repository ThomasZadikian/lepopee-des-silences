namespace Leds.Catalog.Domain.Npcs;

// One trauma. A PNJ has several distinct wounds (decision Q25b). The relationship
// score crossing TenseThreshold / RuptureThreshold drives the gradual state,
// cumulated with direct arming via transgressions.
public sealed record NpcWound(
    string Key,
    EmotionalRegister WoundRegister,
    NpcWoundReversibility Reversibility,
    int TenseThreshold,
    int RuptureThreshold,
    IReadOnlyList<NpcTransgression> Transgressions,
    string? RupturedNarrativeKey = null);