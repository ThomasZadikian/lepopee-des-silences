namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>
/// Runtime-oriented snapshot of a skill template owned by the Catalog service.
/// </summary>
public sealed record SkillTemplateSnapshot(
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string SkillType,
    int Power,
    int Cost,
    string CostType,
    string TargetingMode,
    IReadOnlyCollection<string> EffectTags);