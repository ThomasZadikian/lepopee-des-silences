using Leds.Catalog.Domain.PalaceLaws;

namespace Leds.Catalog.Application.PalaceLaws.Dtos;

public sealed record PalaceLawDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Visibility,
    int Priority,
    IReadOnlyCollection<string> ImpactDomains,
    string? EffectSetKey = null,
    IReadOnlyCollection<PalaceLawEffectDefinitionDto>? Effects = null)
{
    public static PalaceLawDefinitionDto FromDomain(
        IPalaceLawDefinition palaceLaw)
    {
        return new PalaceLawDefinitionDto(
            palaceLaw.Id.Value,
            palaceLaw.Key.Value,
            palaceLaw.Name.Value,
            palaceLaw.Description.Value,
            palaceLaw.Version.Value,
            palaceLaw.Status.ToString(),
            palaceLaw.Visibility.ToString(),
            palaceLaw.Priority,
            palaceLaw.ImpactDomains
                .Select(domain => domain.ToString())
                .ToArray(),
            EffectSetKey: null,
            Effects: []);
    }
}

public sealed record PalaceLawEffectDefinitionDto(
    string EffectType,
    string TargetScope,
    decimal Value,
    string ValueMode,
    string Duration,
    string StackPolicy,
    string? Condition,
    int Order,
    string? BehaviorTag,
    string? GenerationTag,
    string? SelectionGroup);
