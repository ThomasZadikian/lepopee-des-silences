using System.Text.Json;
using Leds.Catalog.Application.Archetypes;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfArchetypeDefinitionReadStore : IArchetypeDefinitionReadStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly CatalogDbContext _context;
    public EfArchetypeDefinitionReadStore(CatalogDbContext context) => _context = context;

    public async Task<ArchetypeDefinitionDto?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.ArchetypeDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == key && item.Status == "Active", cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyCollection<ArchetypeDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken) =>
        (await _context.ArchetypeDefinitions.AsNoTracking().Where(item => item.Status == "Active")
            .OrderBy(item => item.DisplayName).ToArrayAsync(cancellationToken)).Select(Map).ToArray();

    private static ArchetypeDefinitionDto Map(ArchetypeDefinitionEntity entity) => new(
        entity.Key,
        entity.DisplayName,
        entity.Description,
        JsonSerializer.Deserialize<ArchetypeBaseStatsDto>(entity.BaseStatsJson, Json)
            ?? throw new InvalidOperationException($"Archetype '{entity.Key}' has no base stats."),
        JsonSerializer.Deserialize<string[]>(entity.ProficiencyTagsJson, Json) ?? [],
        JsonSerializer.Deserialize<StarterEquipmentDto[]>(entity.StarterEquipmentJson, Json) ?? [],
        JsonSerializer.Deserialize<string[]>(entity.StarterKnownSkillsJson, Json) ?? [],
        JsonSerializer.Deserialize<string[]>(entity.StarterEquippedSkillsJson, Json) ?? []);
}
