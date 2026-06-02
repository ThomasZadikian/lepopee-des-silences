using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.Application.Items.Dtos;

public sealed record ItemTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Category,
    string Rarity,
    string Duration,
    int EffectValue,
    int Price)
{
    public static ItemTemplateDto FromDomain(IItemTemplate item)
    {
        return new ItemTemplateDto(
            item.Id.Value,
            item.Key.Value,
            item.Name.Value,
            item.Description.Value,
            item.Version.Value,
            item.Status.ToString(),
            item.Category.ToString(),
            item.Rarity.ToString(),
            item.Duration.ToString(),
            item.EffectValue,
            item.Price);
    }
}