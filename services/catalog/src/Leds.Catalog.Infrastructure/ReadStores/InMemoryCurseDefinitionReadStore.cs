using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Curses;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryCurseDefinitionReadStore : ICurseDefinitionReadStore, ICurseDefinitionDtoReadStore
{
    private readonly IReadOnlyCollection<CurseDefinitionDto> _definitions =
    [
        new CurseDefinitionDto(
            Guid.NewGuid(),
            "curse.old-wound",
            "1.0.0",
            "Vieille blessure",
            "Une blessure ancienne qui se rouvre au plus mauvais moment.",
            "Le corps porte ses propres souvenirs.",
            3,
            "NextCombatOnly",
            null,
            "effectset.curse-old-wound",
            "Active"),
        new CurseDefinitionDto(
            Guid.NewGuid(),
            "curse.weight-of-silence",
            "1.0.0",
            "Poids du silence",
            "Le silence devient une charge mentale supplémentaire.",
            null,
            5,
            "NextCombatOnly",
            null,
            "effectset.curse-weight-of-silence",
            "Active")
    ];

    public Task<IReadOnlyCollection<ICurseDefinition>> ListAvailableAsync(CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(definition => string.Equals(definition.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(ToDomain)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ICurseDefinition>>(definitions);
    }

    public Task<ICurseDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(definition =>
            string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition is null ? null : ToDomain(definition));
    }

    public Task<IReadOnlyCollection<CurseDefinitionDto>> ListAvailableDtosAsync(CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(definition => string.Equals(definition.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CurseDefinitionDto>>(definitions);
    }

    public Task<CurseDefinitionDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(definition =>
            string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    private static ICurseDefinition ToDomain(CurseDefinitionDto dto)
    {
        return CurseDefinition.Create(
            dto.Key,
            dto.DisplayName,
            dto.Description,
            dto.Version,
            dto.NarrativeText,
            dto.Severity,
            dto.Duration,
            dto.Trigger,
            dto.EffectSetKey,
            Enum.Parse<CatalogContentStatus>(dto.Status));
    }
}
