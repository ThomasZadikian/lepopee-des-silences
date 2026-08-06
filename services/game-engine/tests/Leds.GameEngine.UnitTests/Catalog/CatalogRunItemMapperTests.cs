using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogRunItemMapperTests
{
    [Theory]
    [InlineData("Potion", "Consumable", RunItemType.Consumable)]
    [InlineData("Accessory", "Equipment", RunItemType.Equipment)]
    [InlineData("Weapon", "Equipment", RunItemType.Weapon)]
    [InlineData("Material", "Material", RunItemType.Passive)]
    public void MapType_ShouldApplyExplicitContract(
        string itemType, string category, RunItemType expected)
    {
        CatalogRunItemMapper.MapType(itemType, category).Should().Be(expected);
    }

    [Fact]
    public void MapRarity_ShouldMapCatalogUniqueExplicitly()
    {
        CatalogRunItemMapper.MapRarity("Unique").Should().Be(RunItemRarity.Legendary);
    }

    [Fact]
    public void UnknownValues_ShouldNeverFallbackSilently()
    {
        var type = () => CatalogRunItemMapper.MapType("FutureType", "FutureCategory");
        var rarity = () => CatalogRunItemMapper.MapRarity("Mythic");
        var effect = () => CatalogRunItemMapper.MapEffect("FutureEffect");

        type.Should().Throw<DomainException>();
        rarity.Should().Throw<DomainException>();
        effect.Should().Throw<DomainException>();
    }
}
