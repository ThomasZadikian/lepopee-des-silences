using FluentAssertions;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Tests verifying that selecting a Guard-typed item reward creates
/// a StartingGuardBonus RunModifier that is applied by CombatFactory.
/// </summary>
public sealed class RunGuardShardTests
{
    // Guard-shard payload mirrors RewardOfferFactory.CreateMerchantRewardChoices
    private const string GuardShardPayload =
        "item:item.consumable.guard-shard:Éclat de garde:Offre une protection au combat.:Consumable:Uncommon:Guard:8";

    // -----------------------------------------------------------------------
    // Core: Guard item → RunModifier created
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyGuardItemReward_ShouldAddStartingGuardBonusModifier()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        run.RunModifiers.Should().ContainSingle(
            m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed,
            because: "Selecting a Guard item must create a StartingGuardBonus RunModifier.");
    }

    [Fact]
    public void ApplyGuardItemReward_ShouldCreateModifierWithCorrectValue()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        var modifier = run.RunModifiers
            .Single(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed);

        modifier.Value.Should().Be(8,
            because: "Guard shard gives +8 guard as declared in the payload.");
    }

    [Fact]
    public void ApplyGuardItemReward_ShouldCreateModifierWithUntilRunEndsDuration()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        var modifier = run.RunModifiers
            .Single(m => m.Type == RunModifierType.StartingGuardBonus);

        modifier.Duration.Should().Be(RunModifierDuration.UntilRunEnds,
            because: "Guard bonus is a permanent run-scoped bonus.");
    }

    [Fact]
    public void ApplyGuardItemReward_ShouldSetSourceTypeToRunItem()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        var modifier = run.RunModifiers
            .Single(m => m.Type == RunModifierType.StartingGuardBonus);

        modifier.SourceType.Should().Be("RunItem");
        modifier.SourceKey.Should().Be("item.consumable.guard-shard");
    }

    // -----------------------------------------------------------------------
    // Stacking
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyGuardItemTwice_ShouldStackToSixteen()
    {
        var run = TestGameEngineFactory.CreateRun();

        SetupPendingGuardReward(run);
        SetupPendingGuardReward(run);

        var totalGuardBonus = run.RunModifiers
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => m.Value);

        totalGuardBonus.Should().Be(16,
            because: "Two guard shards each giving +8 should stack to +16.");
    }

    // -----------------------------------------------------------------------
    // Cap
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyGuardItems_ShouldRespectMaxStartingGuardBonusOf30()
    {
        var run = TestGameEngineFactory.CreateRun();

        // Apply 4 × 8 = 32 guard; should be capped at 30.
        for (var i = 0; i < 4; i++)
        {
            SetupPendingGuardReward(run);
        }

        var totalGuardBonus = run.RunModifiers
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => m.Value);

        totalGuardBonus.Should().BeLessThanOrEqualTo(30,
            because: "StartingGuardBonus is capped at 30 across all stacks.");
    }

    [Fact]
    public void ApplyGuardItem_WhenCapAlreadyReached_ShouldNotAddMoreModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();

        // Fill the cap exactly (4 × 8 = 32 → capped at 30, so item 4 adds only 6)
        for (var i = 0; i < 3; i++)
        {
            SetupPendingGuardReward(run); // 3 × 8 = 24
        }
        // 4th item: cap at 30 → adds only 6
        SetupPendingGuardReward(run);

        var bonusBefore = run.RunModifiers
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => m.Value);

        bonusBefore.Should().Be(30);

        // 5th item: already at cap, should add nothing
        SetupPendingGuardReward(run);

        var bonusAfter = run.RunModifiers
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => m.Value);

        bonusAfter.Should().Be(30,
            because: "Once the cap is reached, additional guard items must not increase the bonus.");
    }

    // -----------------------------------------------------------------------
    // RunItem is also added to inventory
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyGuardItemReward_ShouldAlsoAddRunItemToInventory()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        run.RunItems.Should().ContainSingle(
            i => i.DefinitionKey == "item.consumable.guard-shard",
            because: "Guard shard must appear in the inventory.");
    }

    // -----------------------------------------------------------------------
    // Modifier survives combat (not consumed by ConsumeNextCombatModifiers)
    // -----------------------------------------------------------------------

    [Fact]
    public void StartingGuardBonus_ShouldNotBeConsumedByConsumeNextCombatModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();
        SetupPendingGuardReward(run);

        run.ConsumeNextCombatModifiers();

        run.RunModifiers
            .Single(m => m.Type == RunModifierType.StartingGuardBonus)
            .IsConsumed.Should().BeFalse(
                because: "StartingGuardBonus has UntilRunEnds duration and must survive combat end.");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Injects a guard-shard reward choice into the run and applies it,
    /// bypassing the full RewardOffer flow to stay unit-test-scoped.
    /// Clears the pending reward after application so the method is idempotent.
    /// </summary>
    private static void SetupPendingGuardReward(Run run)
    {
        var offerId = RewardOfferId.New();
        run.SetPendingRewardOffer(offerId);

        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Éclat de garde",
            "Offre une protection au combat.",
            GuardShardPayload);

        run.ApplyReward(choice);
        run.ClearPendingRewardOffer();
    }
}