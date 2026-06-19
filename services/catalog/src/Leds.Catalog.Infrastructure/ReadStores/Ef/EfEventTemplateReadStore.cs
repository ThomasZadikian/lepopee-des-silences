using System.Text.Json;
using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.EventTemplates;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfEventTemplateReadStore : IEventTemplateReadStore
{
    private readonly CatalogDbContext _context;

    public EfEventTemplateReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IEventTemplate>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.EventTemplates
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IEventTemplate?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.EventTemplates
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IEventTemplate MapToDomain(EventTemplateEntity entity)
    {
        var narrativeTags = JsonSerializer.Deserialize<List<string>>(entity.NarrativeTagsJson) ?? [];

        return EventTemplate.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<EventTemplateType>(entity.Type),
            Enum.Parse<EventOutcomeKind>(entity.DefaultOutcomeKind),
            entity.MinRiskLevel,
            entity.MaxRiskLevel,
            entity.RequiresPlayerChoice,
            narrativeTags,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
