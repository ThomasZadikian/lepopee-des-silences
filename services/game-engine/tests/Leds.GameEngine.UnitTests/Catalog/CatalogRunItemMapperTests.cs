using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogRunItemMapperTests
{
    [Theory]
    [InlineData("Consumable", RunItemType.Consumable)]
    [InlineData("Equipment", RunItemType.Equipment)]
    [InlineData("Weapon", RunItemType.Weapon)]
    [InlineData("Grimoire", RunItemType.Grimoire)]
    [InlineData("WeatherInstrument", RunItemType.WeatherInstrument)]
    [InlineData("SkillEssence", RunItemType.SkillEssence)]
    [InlineData("Relic", RunItemType.Relic)]
    [InlineData("Material", RunItemType.Passive)]
    [InlineData("Key", RunItemType.Passive)]
    public void MapType_ShouldResolveFromCategoryAlone(string category, RunItemType expected)
    {
        CatalogRunItemMapper.MapType(category).Should().Be(expected);
    }

    [Fact]
    public void MapType_ShouldRejectCurrency_NoRuntimeRepresentation()
    {
        var act = () => CatalogRunItemMapper.MapType("Currency");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TryMapType_ShouldReturnFalse_ForCurrency()
    {
        CatalogRunItemMapper.TryMapType("Currency", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(RunItemType.Weapon, "Weapon")]
    [InlineData(RunItemType.Equipment, "Accessory")]
    [InlineData(RunItemType.Relic, "Relic")]
    [InlineData(RunItemType.Grimoire, "Relic")]
    [InlineData(RunItemType.WeatherInstrument, "Relic")]
    [InlineData(RunItemType.SkillEssence, "Relic")]
    public void MapEquipSlot_ShouldResolveEquippableKinds_WhenUsageModeIsEquip(RunItemType type, string expectedSlot)
    {
        CatalogRunItemMapper.MapEquipSlot(type, "Equip").Should().Be(expectedSlot);
    }

    [Theory]
    [InlineData(RunItemType.Consumable)]
    [InlineData(RunItemType.Passive)]
    [InlineData(RunItemType.Fragment)]
    public void MapEquipSlot_ShouldReturnNull_ForNonEquippableKinds(RunItemType type)
    {
        CatalogRunItemMapper.MapEquipSlot(type, "Equip").Should().BeNull();
    }

    [Theory]
    [InlineData(RunItemType.Weapon)]
    [InlineData(RunItemType.Equipment)]
    [InlineData(RunItemType.Relic)]
    [InlineData(RunItemType.Grimoire)]
    [InlineData(RunItemType.WeatherInstrument)]
    [InlineData(RunItemType.SkillEssence)]
    public void MapEquipSlot_ShouldReturnNull_ForEquippableKinds_WhenUsageModeIsNotEquip(RunItemType type)
    {
        // A category that is structurally equip-slot-shaped (e.g. SkillEssence -> Relic)
        // must not offer an equip action for an item actually meant to be consumed
        // once (UsageMode "UseOutsideCombat"/"UseInCombat"/etc.) — usageMode is the
        // deciding signal, not the category collapse table alone.
        CatalogRunItemMapper.MapEquipSlot(type, "UseOutsideCombat").Should().BeNull();
        CatalogRunItemMapper.MapEquipSlot(type, null).Should().BeNull();
    }

    [Fact]
    public void MapRarity_ShouldMapCatalogUniqueExplicitly()
    {
        CatalogRunItemMapper.MapRarity("Unique").Should().Be(RunItemRarity.Legendary);
    }

    [Fact]
    public void UnknownValues_ShouldNeverFallbackSilently()
    {
        var type = () => CatalogRunItemMapper.MapType("FutureCategory");
        var rarity = () => CatalogRunItemMapper.MapRarity("Mythic");
        var effect = () => CatalogRunItemMapper.MapEffect("FutureEffect");

        type.Should().Throw<DomainException>();
        rarity.Should().Throw<DomainException>();
        effect.Should().Throw<DomainException>();
    }
}
