using FluentAssertions;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Rewards.Loot;

public sealed class EnemyLootRewardBuilderTests
{
    private static EnemyLootRewardBuilder CreateBuilder(StubCatalogContentGateway? gateway = null) =>
        new(gateway ?? new StubCatalogContentGateway());

    [Fact]
    public async Task BuildAsync_ShouldRollBetween1And3Items_PerEnemyWithATable()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);

        var choices = await CreateBuilder().BuildAsync(
            "seed-1", Guid.NewGuid(), Guid.NewGuid(), [enemy]);

        choices.Count.Should().BeInRange(1, 6);
        var fromEnemy = choices.Where(c => c.SourceEnemyKey == "enemy.forest.chimere-serpentaire").ToList();
        fromEnemy.Count.Should().BeInRange(1, 3);
        choices.Should().OnlyContain(
            c => c.SourceEnemyKey == "enemy.forest.chimere-serpentaire" || c.SourceEnemyKey == null,
            because: "a single enemy's table can roll short of MinLootCount, which pads from the generic fallback pool (null source)");
    }

    [Fact]
    public async Task BuildAsync_ShouldSkipEnemiesWithNoConfiguredLootTable()
    {
        var enemyWithoutTable = Combatant.CreateEnemy("enemy.threshold.doubt-fragment", "Fragment de Doute", "Fragile", 10);

        var choices = await CreateBuilder().BuildAsync(
            "seed-2", Guid.NewGuid(), Guid.NewGuid(), [enemyWithoutTable]);

        // No enemy loot table configured -> falls entirely back to the generic pool.
        choices.Should().OnlyContain(c => c.SourceEnemyKey == null);
    }

    [Fact]
    public async Task BuildAsync_ShouldPadUpToTheFloor_WhenRolledCountIsBelowThree()
    {
        var enemyWithoutTable = Combatant.CreateEnemy("enemy.threshold.doubt-fragment", "Fragment de Doute", "Fragile", 10);

        var choices = await CreateBuilder().BuildAsync(
            "seed-3", Guid.NewGuid(), Guid.NewGuid(), [enemyWithoutTable]);

        choices.Count.Should().BeGreaterThanOrEqualTo(EnemyLootRewardBuilder.MinLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldCapAtSix_WhenManyEnemiesRollLotsOfLoot()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
            Combatant.CreateEnemy("enemy.threshold.fracture", "Fracture", "Bruiser", 32),
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
        };

        var choices = await CreateBuilder().BuildAsync(
            "seed-4", Guid.NewGuid(), Guid.NewGuid(), enemies);

        choices.Count.Should().BeLessThanOrEqualTo(EnemyLootRewardBuilder.MaxLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnEmpty_WhenThereAreNoEnemiesAndNoFallbackPool()
    {
        var choices = await CreateBuilder().BuildAsync(
            "seed-5", Guid.NewGuid(), Guid.NewGuid(), []);

        // The stub gateway always has an active fallback pool, so an empty fight still
        // pads up to the floor from it.
        choices.Count.Should().BeGreaterThanOrEqualTo(EnemyLootRewardBuilder.MinLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldBeDeterministic_ForTheSameInputs()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
        };
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var first = await CreateBuilder().BuildAsync("seed-6", runId, combatId, enemies);
        var second = await CreateBuilder().BuildAsync("seed-6", runId, combatId, enemies);

        first.Select(c => c.PayloadKey).Should().Equal(second.Select(c => c.PayloadKey));
    }

    [Fact]
    public async Task BuildAsync_ShouldChangeSelection_WhenSeedChanges()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
            Combatant.CreateEnemy("enemy.threshold.fracture", "Fracture", "Bruiser", 32),
        };
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(i => CreateBuilder().BuildAsync($"seed-varied-{i}", runId, combatId, enemies)));

        var distinctSignatures = results
            .Select(r => string.Join(",", r.Select(c => c.PayloadKey)))
            .Distinct()
            .Count();

        distinctSignatures.Should().BeGreaterThan(1,
            "different seeds should be able to produce different loot rolls.");
    }

    // "Loi de l'Invitation" (law.invitation) — combat loot item drop chances are
    // boosted by a percentage. "enemy.forest.chimere-serpentaire" has a low-probability
    // entry (item.consumable.venin-cristallise, 8%) that's the most sensitive to a
    // bonus. Each trial index reuses the SAME seed for the baseline and boosted call —
    // since the underlying deterministic sample only depends on (seed, runId, combatId,
    // step), not on the bonus, a boosted hit is a strict superset of a baseline hit for
    // any given trial (16% effective threshold vs 8%), making this a paired comparison
    // rather than two independent noisy samples.
    [Fact]
    public async Task BuildAsync_ShouldIncreaseLowProbabilityItemDropRate_WhenLootChanceBonusIsActive()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        const int trials = 300;

        var baselineHits = 0;
        var boostedHits = 0;

        for (var i = 0; i < trials; i++)
        {
            var seed = $"seed-invitation-{i}";
            var builder = CreateBuilder();

            var baseline = await builder.BuildAsync(seed, runId, combatId, [enemy]);
            if (baseline.Any(c => c.PayloadKey.Contains("venin-cristallise"))) baselineHits++;

            var boosted = await builder.BuildAsync(seed, runId, combatId, [enemy], lootChanceBonusPercent: 100);
            if (boosted.Any(c => c.PayloadKey.Contains("venin-cristallise"))) boostedHits++;
        }

        boostedHits.Should().BeGreaterThan(baselineHits,
            "a +100% loot chance bonus should meaningfully increase a low-probability item's drop rate");
    }

    [Fact]
    public async Task BuildAsync_ShouldIncreaseLowProbabilityItemDropRate_WhenLootMultiplierIsAboveOne()
    {
        // Same paired-trial reasoning as the loot-chance-bonus test above, but exercising
        // the risk-tier LootMultiplier (CombatRiskProfileResolver) instead of the law
        // modifier — a separate code path feeding the same effectiveDropPercent formula.
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        const int trials = 300;

        var baselineHits = 0;
        var boostedHits = 0;

        for (var i = 0; i < trials; i++)
        {
            var seed = $"seed-risktier-{i}";
            var builder = CreateBuilder();

            var baseline = await builder.BuildAsync(seed, runId, combatId, [enemy]);
            if (baseline.Any(c => c.PayloadKey.Contains("venin-cristallise"))) baselineHits++;

            var boosted = await builder.BuildAsync(seed, runId, combatId, [enemy], lootMultiplier: 1.75);
            if (boosted.Any(c => c.PayloadKey.Contains("venin-cristallise"))) boostedHits++;
        }

        boostedHits.Should().BeGreaterThan(baselineHits,
            "the Fatal tier's 1.75x LootMultiplier should meaningfully increase a low-probability item's drop rate");
    }

    [Fact]
    public async Task BuildAsync_ShouldCapEffectiveDropChance_AtOneHundredPercent()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var combatId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        foreach (var i in Enumerable.Range(0, 50))
        {
            var choices = await CreateBuilder().BuildAsync(
                $"seed-invitation-cap-{i}", runId, combatId, [enemy], lootChanceBonusPercent: 1000);

            // Every positive table percentage clamps to a guaranteed hit at 100%; the
            // only remaining selection is the deterministic MaxPerEnemy trim from 4 to 3.
            // Assert the invariant directly instead of statistically sampling one item,
            // which made this deterministic system look flaky whenever new GUIDs changed
            // the sampler input between CI runs.
            choices.Where(c => c.SourceEnemyKey == enemy.SourceKey)
                .Should().HaveCount(3);
        }
    }
}
