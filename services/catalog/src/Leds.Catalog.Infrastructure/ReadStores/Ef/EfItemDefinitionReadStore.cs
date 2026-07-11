using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Application.Items.Definitions.Dtos;
using Leds.Catalog.Application.Items.Definitions.Ports;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfItemDefinitionReadStore : IItemTemplateReadStore, IItemDefinitionReadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public EfItemDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IItemTemplate>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.ItemDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IItemTemplate?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.ItemDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<ItemDefinitionDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.ItemDefinitions
            .Include(e => e.EffectSet)
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyCollection<ItemDefinitionDto>> ListActiveDtosAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.ItemDefinitions
            .Include(e => e.EffectSet)
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToArray();
    }

    private static IItemTemplate MapToDomain(ItemDefinitionEntity entity)
    {
        return ItemTemplate.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<ItemCategory>(entity.Category),
            Enum.Parse<ItemRarity>(entity.Rarity),
            Enum.Parse<ItemDuration>(entity.Duration),
            entity.EffectValue,
            entity.Price,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }

    private static ItemDefinitionDto MapToDto(ItemDefinitionEntity entity)
    {
        var equipmentEffects = JsonSerializer.Deserialize<List<ItemEquipmentEffect>>(
            entity.EquipmentEffectsJson ?? "[]", JsonOptions) ?? [];
        var readablePages = JsonSerializer.Deserialize<List<string>>(
            entity.ReadablePagesJson ?? "[]", JsonOptions) ?? [];

        return new ItemDefinitionDto(
            entity.Id,
            entity.Key,
            entity.Version,
            string.IsNullOrWhiteSpace(entity.DisplayName) ? entity.Name : entity.DisplayName,
            entity.Description,
            entity.NarrativeText,
            entity.Category,
            entity.ItemType,
            entity.Rarity,
            entity.UsageMode,
            entity.Lifecycle,
            entity.StackPolicy,
            entity.MaxStack,
            entity.IsUsableInCombat,
            entity.IsUsableOutsideCombat,
            entity.EffectSet?.Key,
            entity.Status,
            IsPermanentEligible: string.Equals(entity.Duration, nameof(ItemDuration.Permanent), StringComparison.OrdinalIgnoreCase),
            EquipmentEffects: equipmentEffects.Select(ItemEquipmentEffectDto.FromDomain).ToArray(),
            IsContainer: entity.IsContainer,
            ContainerCapacity: entity.ContainerCapacity,
            IsLiquid: entity.IsLiquid,
            EffectValue: entity.EffectValue,
            EffectRunType: entity.EffectRunType,
            ReadablePages: readablePages);
    }
}
