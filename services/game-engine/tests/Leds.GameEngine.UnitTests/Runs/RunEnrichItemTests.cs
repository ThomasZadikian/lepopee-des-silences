using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunEnrichItemTests
{
    [Fact]
    public void EnrichLastAddedItem_ShouldSetCatalogFields_WhenItemExists()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "item.consumable.minor-heal", "Baume", "",
            RunItemType.Consumable, RunItemRarity.Common, 1,
            RunItemEffectType.Heal, 15);
        run.AddRunItem(item);

        run.EnrichLastAddedItem(
            definitionVersion: "1.0",
            narrativeText: "Un baume ancien.",
            category: "Consumable",
            usageMode: "UseInCombat",
            lifecycle: "RuntimeRunOnly",
            maxStack: 99,
            isUsableInCombat: true,
            isUsableOutsideCombat: true,
            sourceRewardOptionId: Guid.NewGuid());

        var enriched = run.RunItems.Last();
        enriched.DefinitionVersion.Should().Be("1.0");
        enriched.NarrativeText.Should().Be("Un baume ancien.");
        enriched.Category.Should().Be("Consumable");
        enriched.UsageMode.Should().Be("UseInCombat");
        enriched.Lifecycle.Should().Be("RuntimeRunOnly");
        enriched.MaxStack.Should().Be(99);
        enriched.SourceRewardOptionId.Should().NotBeNull();
    }

    [Fact]
    public void EnrichLastAddedItem_ShouldNotThrow_WhenRunHasNoItems()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.EnrichLastAddedItem(
            "1.0", null, "Consumable", "UseInCombat", "RuntimeRunOnly",
            99, true, true, Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichLastAddedItem_ShouldPreserveOriginalFieldValues_WhenNotOverridden()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "item.test", "Test", "Original desc",
            RunItemType.Consumable, RunItemRarity.Common, 1,
            RunItemEffectType.Heal, 10);
        run.AddRunItem(item);

        run.EnrichLastAddedItem(
            definitionVersion: "2.0",
            narrativeText: null,
            category: "Consumable",
            usageMode: "UseInCombat",
            lifecycle: "RuntimeRunOnly",
            maxStack: 5,
            isUsableInCombat: true,
            isUsableOutsideCombat: false,
            sourceRewardOptionId: Guid.NewGuid());

        var enriched = run.RunItems.Last();
        enriched.DefinitionKey.Should().Be("item.test");
        enriched.DisplayName.Should().Be("Test");
        enriched.Description.Should().Be("Original desc");
        enriched.EffectType.Should().Be(RunItemEffectType.Heal);
        enriched.EffectAmount.Should().Be(10);
    }

    [Fact]
    public void EnrichLastAddedItem_ShouldNotAffectOtherItems()
    {
        var run = TestGameEngineFactory.CreateRun();
        var firstItem = RunItem.Create("item.first", "First", "", RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Heal, 5);
        var secondItem = RunItem.Create("item.second", "Second", "", RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Guard, 8);
        run.AddRunItem(firstItem);
        run.AddRunItem(secondItem);

        run.EnrichLastAddedItem(
            "1.0", null, "Consumable", "UseInCombat", "RuntimeRunOnly",
            99, true, true, Guid.NewGuid());

        run.RunItems.First().DefinitionVersion.Should().BeNull();
        run.RunItems.Last().DefinitionVersion.Should().Be("1.0");
    }

    [Fact]
    public void EnrichLastAddedItem_ShouldSetSourceRewardOptionId()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create("item.test", "Test", "", RunItemType.Consumable, RunItemRarity.Common, 1, RunItemEffectType.Heal, 10);
        run.AddRunItem(item);

        var sourceId = Guid.NewGuid();
        run.EnrichLastAddedItem(
            "1.0", null, "Consumable", "UseInCombat", "RuntimeRunOnly",
            99, true, true, sourceId);

        run.RunItems.Last().SourceRewardOptionId.Should().Be(sourceId);
    }
}
