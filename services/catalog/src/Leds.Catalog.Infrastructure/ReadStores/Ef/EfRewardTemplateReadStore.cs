using Leds.Catalog.Application.RewardTemplates.Dtos;
using Leds.Catalog.Application.RewardTemplates.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRewardTemplateReadStore : IRewardTemplateReadStore
{
    private readonly CatalogDbContext _context;

    public EfRewardTemplateReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<RewardTemplateDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await QueryTemplates()
            .FirstOrDefaultAsync(template => template.Key == key, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyCollection<RewardTemplateDto>> ListEligibleDtosAsync(
        RewardTemplateEligibilityQueryContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.SourceType))
        {
            return [];
        }

        var sourceType = context.SourceType.Trim();
        var entities = await QueryTemplates()
            .Where(template => template.Status == "Active" && template.SourceType == sourceType)
            .OrderBy(template => template.Key)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToArray();
    }

    private IQueryable<RewardTemplateEntity> QueryTemplates()
    {
        return _context.RewardTemplates
            .Include(template => template.Options)
            .ThenInclude(option => option.EffectSet);
    }

    private static RewardTemplateDto MapToDto(RewardTemplateEntity entity)
    {
        return new RewardTemplateDto(
            entity.Id,
            entity.Key,
            entity.Version,
            entity.DisplayName,
            entity.Description,
            entity.SourceType,
            entity.MinOptions,
            entity.MaxOptions,
            entity.Status,
            entity.Options
                .OrderBy(option => option.Id)
                .Select(MapOptionToDto)
                .ToArray());
    }

    private static RewardTemplateOptionDto MapOptionToDto(RewardTemplateOptionEntity entity)
    {
        return new RewardTemplateOptionDto(
            entity.RewardType,
            entity.Label,
            entity.Description,
            entity.PayloadKey,
            entity.PayloadKey?.StartsWith("item.", StringComparison.OrdinalIgnoreCase) == true ? "Item" : null,
            entity.EffectSet?.Key,
            entity.BaseAmount ?? 0,
            entity.ScalingMode,
            entity.Weight);
    }
}
