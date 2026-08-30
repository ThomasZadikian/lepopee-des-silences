namespace Leds.Catalog.Domain.Items;

// The single closed vocabulary an item's runtime behavior is classified by — the only
// field the game-engine's CatalogRunItemMapper reads to resolve RunItemType. Weapon/
// Grimoire/WeatherInstrument/SkillEssence used to be expressible only through the
// free-text ItemType field (now FlavorTag, purely narrative); they are first-class
// values here so Category alone is always sufficient, with no secondary field to
// consult. Key/Currency/Material have no distinct runtime item behavior of their own
// (see CatalogRunItemMapper: Key/Material collapse to RunItemType.Passive, Currency
// never reaches a RunItem at all) but stay separate catalog-side categories because
// they are meaningfully different things to author and price.
public enum ItemCategory
{
    Consumable = 0,
    Equipment = 1,
    Relic = 2,
    Key = 3,
    Currency = 4,
    Material = 5,
    Weapon = 6,
    Grimoire = 7,
    WeatherInstrument = 8,
    SkillEssence = 9
}