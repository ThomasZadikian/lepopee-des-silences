namespace Leds.GameEngine.Application.Catalog;

public sealed record SkillDefinitionView(
    string Key,
    string DisplayName,
    string Description,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower);

public sealed record ListActiveSkillDefinitionsResponse(IReadOnlyCollection<SkillDefinitionView> Skills);
