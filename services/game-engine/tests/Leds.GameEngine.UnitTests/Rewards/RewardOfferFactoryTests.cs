using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardOfferFactoryTests
{
    private static RewardOfferFactory CreateFactory(ICatalogContentGateway? catalogContentGateway = null)
    {
        var gateway = catalogContentGateway ?? Mock.Of<ICatalogContentGateway>();
        return new RewardOfferFactory(
            new CombatRiskProfileResolver(),
            gateway,
            new EnemyLootRewardBuilder(gateway));
    }

    // -----------------------------------------------------------------------
    // Basic structural tests (pre-existing, updated for new signature)
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateCombatRewardOffer_ShouldReturnOfferWithThreeChoices()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 2);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.State.Should().Be(RewardOfferState.Pending);
        offer.Choices.Should().HaveCount(3);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldIncludeHealChoice()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 2);

        offer.Choices.Should().Contain(choice => choice.RewardType == RewardType.Heal);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldExposeHealAndItemChoices()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 3);

        offer.Choices.Should().Contain(choice =>
            choice.RewardType == RewardType.Heal ||
            choice.RewardType == RewardType.TemporaryItem);
    }

    // -----------------------------------------------------------------------
    // Tier-aware reward profiles (pre-existing)
    // -----------------------------------------------------------------------

    [Fact]
    public void CombatOutcome_ShouldUseNormalRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 3);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "MVP combat rewards expose heal and item choices.");
    }

    [Fact]
    public void RareCombatOutcome_ShouldUseRareRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Rare, NodeEventType.Rare, riskLevel: 3);

        offer.Source.Should().Be(RewardSource.Rare);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Rare rewards expose heal and item choices.");
    }

    [Fact]
    public void EliteCombatOutcome_ShouldUseEliteRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 4);

        offer.Source.Should().Be(RewardSource.Elite);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Elite rewards expose heal and item choices.");
    }

    [Fact]
    public void RoomBossOutcome_ShouldUseBossRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.RoomBoss, NodeEventType.RoomBoss, riskLevel: 5);

        offer.Source.Should().Be(RewardSource.RoomBoss);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Boss rewards expose heal and item choices.");
    }

    // -----------------------------------------------------------------------
    // CombatScaling metadata — flat per-tier lookup, no more raw-delta formula
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForCalmeTier()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 1);

        offer.CombatScaling.Should().NotBeNull(
            "CombatScaling must be populated for all combat offers.");

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Normal);
        scaling.RiskTier.Should().Be(RiskTier.Calme);
        scaling.DifficultyMultiplier.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForRareCombat()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Rare, NodeEventType.Rare, riskLevel: 4);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Rare);
        scaling.RiskTier.Should().Be(RiskTier.Perilleux);
        scaling.DifficultyMultiplier.Should().BeApproximately(1.60, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForEliteCombat()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 4);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Elite);
        scaling.RiskTier.Should().Be(RiskTier.Perilleux);
        scaling.DifficultyMultiplier.Should().BeApproximately(1.60, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForRoomBossCombat()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.RoomBoss, NodeEventType.RoomBoss, riskLevel: 5);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.RoomBoss);
        scaling.RiskTier.Should().Be(RiskTier.Fatal);
        scaling.DifficultyMultiplier.Should().BeApproximately(2.00, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldHaveMultiplierOne_ForCalmeTier_RegardlessOfEncounterType()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 1);

        offer.CombatScaling!.DifficultyMultiplier.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldUseTheMaxMultiplier_AtFatalTier()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 5);

        offer.CombatScaling!.DifficultyMultiplier.Should().BeApproximately(2.00, 0.001,
            "Fatal is the highest tier, capping the multiplier table.");
    }

    // -----------------------------------------------------------------------
    // CreateCombatRewardOfferAsync — enemy loot tables
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldRollLootFromEnemyTable_AndTagItsSource()
    {
        var gateway = new StubCatalogContentGateway();
        var factory = CreateFactory(gateway);
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);

        var offer = await factory.CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: [enemy], runSeed: "seed-a", runId: Guid.NewGuid(), combatId: Guid.NewGuid());

        offer.Choices.Should().NotBeEmpty();
        offer.Choices.Should().OnlyContain(c => c.RewardType == RewardType.TemporaryItem);
        offer.Choices.Should().Contain(c => c.SourceEnemyDisplayName == "Chimere Serpentaire");
    }

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldStayWithinFloorAndCap()
    {
        var gateway = new StubCatalogContentGateway();
        var factory = CreateFactory(gateway);
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
            Combatant.CreateEnemy("enemy.threshold.fracture", "Fracture", "Bruiser", 32),
        };

        var offer = await factory.CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: enemies, runSeed: "seed-b", runId: Guid.NewGuid(), combatId: Guid.NewGuid());

        offer.Choices.Count.Should().BeInRange(3, 6);
    }

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldPadFromFallback_WhenNoEnemyHasALootTable()
    {
        var gateway = new StubCatalogContentGateway();
        var factory = CreateFactory(gateway);
        var enemy = Combatant.CreateEnemy("enemy.threshold.echo", "Echo", "Fragile", 20);

        var offer = await factory.CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: [enemy], runSeed: "seed-c", runId: Guid.NewGuid(), combatId: Guid.NewGuid());

        offer.Choices.Count.Should().BeGreaterThanOrEqualTo(3);
        offer.Choices.Should().OnlyContain(c => c.SourceEnemyDisplayName == null,
            because: "the enemy has no loot table, so every item comes from the generic fallback pool.");
    }

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldBeDeterministic_ForTheSameSeedRunAndCombat()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
        };
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var first = await CreateFactory(new StubCatalogContentGateway()).CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: enemies, runSeed: "seed-d", runId: runId, combatId: combatId);

        var second = await CreateFactory(new StubCatalogContentGateway()).CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: enemies, runSeed: "seed-d", runId: runId, combatId: combatId);

        first.Choices.Select(c => c.PayloadKey).Should().Equal(second.Choices.Select(c => c.PayloadKey));
    }

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldFallBackToHardcodedChoices_WhenNoLootIsAvailableAtAll()
    {
        var gateway = Mock.Of<ICatalogContentGateway>();
        var factory = CreateFactory(gateway);

        var offer = await factory.CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: [], runSeed: "seed-e", runId: Guid.NewGuid(), combatId: Guid.NewGuid());

        offer.Choices.Should().HaveCount(3,
            because: "with no enemies and no fallback pool configured, the factory falls back to the hardcoded tier choices.");
    }

    // -----------------------------------------------------------------------
    // "Loi de l'Abondance" (law.abondance) — item nodes propose a 4th choice while
    // RunModifierType.AbondanceExtraChoiceEnabled is active. Documented simplification:
    // the "un nœud sur deux est vide" half is not modeled — see RewardOfferFactory.
    // -----------------------------------------------------------------------

    // These 3 hit the unconfigured Mock.Of<ICatalogContentGateway>() default from
    // CreateFactory(), so GetRewardTemplateByKeyAsync returns null and
    // CreateItemRewardOfferAsync falls back to the small hardcoded pool (see
    // RewardOfferFactory.CreateItemRewardChoices) — exercised directly by the
    // catalog-driven tests further below.
    [Fact]
    public async Task CreateItemRewardOffer_ShouldReturnThreeChoices_WhenAbondanceIsNotActive()
    {
        var offer = await CreateFactory().CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateItemRewardOffer_ShouldReturnFourChoices_WhenAbondanceIsActive()
    {
        var modifier = RunModifier.Create(
            RunModifierType.AbondanceExtraChoiceEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-abondance-test");

        var offer = await CreateFactory().CreateItemRewardOfferAsync(
            "default", riskLevel: 25, [modifier], "seed", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateItemRewardOffer_ShouldReturnThreeChoices_WhenAbondanceModifierIsConsumed()
    {
        var modifier = RunModifier.Create(
            RunModifierType.AbondanceExtraChoiceEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-abondance-test");
        modifier.Consume(DateTime.UtcNow);

        var offer = await CreateFactory().CreateItemRewardOfferAsync(
            "default", riskLevel: 25, [modifier], "seed", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().HaveCount(3);
    }

    // -----------------------------------------------------------------------
    // "Loi de l'Invitation" (law.invitation) — combat loot item drop chances are
    // boosted while RunModifierType.LootChanceBonusPercent is active. The bonus math
    // itself is covered thoroughly in EnemyLootRewardBuilderTests; here we only verify
    // CreateCombatRewardOfferAsync actually threads run.RunModifiers through.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldIncreaseLowProbabilityItemDropRate_WhenLootChanceBonusModifierIsActive()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        var modifier = RunModifier.Create(
            RunModifierType.LootChanceBonusPercent, 100, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-invitation-test");
        const int trials = 100;

        var baselineHits = 0;
        var boostedHits = 0;

        for (var i = 0; i < trials; i++)
        {
            var seed = $"seed-invitation-factory-{i}";
            var factory = CreateFactory(new StubCatalogContentGateway());

            var baseline = await factory.CreateCombatRewardOfferAsync(
                RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
                enemies: [enemy], runSeed: seed, runId: runId, combatId: combatId);
            if (baseline.Choices.Any(c => c.PayloadKey.Contains("venin-cristallise"))) baselineHits++;

            var boosted = await factory.CreateCombatRewardOfferAsync(
                RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
                enemies: [enemy], runSeed: seed, runId: runId, combatId: combatId,
                cancellationToken: CancellationToken.None, runModifiers: [modifier]);
            if (boosted.Choices.Any(c => c.PayloadKey.Contains("venin-cristallise"))) boostedHits++;
        }

        boostedHits.Should().BeGreaterThan(baselineHits,
            "an active LootChanceBonusPercent modifier should reach the loot builder and increase drop rates");
    }

    [Fact]
    public async Task CreateCombatRewardOfferAsync_ShouldIgnoreLootChanceBonusModifier_WhenConsumed()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();
        var modifier = RunModifier.Create(
            RunModifierType.LootChanceBonusPercent, 1000, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-invitation-test");
        modifier.Consume(DateTime.UtcNow);

        var withoutModifier = await CreateFactory(new StubCatalogContentGateway()).CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: [enemy], runSeed: "seed-invitation-consumed", runId: runId, combatId: combatId);

        var withConsumedModifier = await CreateFactory(new StubCatalogContentGateway()).CreateCombatRewardOfferAsync(
            RewardSource.Combat, NodeEventType.Combat, riskLevel: 3,
            enemies: [enemy], runSeed: "seed-invitation-consumed", runId: runId, combatId: combatId,
            cancellationToken: CancellationToken.None, runModifiers: [modifier]);

        // Same seed/run/combat, so the only possible difference is the modifier — a
        // consumed one must be excluded from the sum, leaving the roll unchanged even
        // at an enormous 1000% bonus.
        withConsumedModifier.Choices.Select(c => c.PayloadKey)
            .Should().Equal(withoutModifier.Choices.Select(c => c.PayloadKey));
    }

    // -----------------------------------------------------------------------
    // "Loi de la Chandelle" (law.chandelle) — item-node offers are deterministically
    // sampled from the catalog-authored "reward.item.default" template pool, so that a
    // reroll (a different rerollNonce) can draw a genuinely different subset once the
    // pool holds more options than are shown at once.
    // -----------------------------------------------------------------------

    private static CatalogRewardTemplateOptionSnapshot CreateOption(string key, int weight = 1) => new(
        RewardType: "TemporaryItem",
        Label: $"Objet {key}",
        Description: $"Description {key}",
        PayloadKey: $"item.consumable.{key}",
        PayloadType: "Item",
        EffectSetKey: null,
        BaseAmount: 10,
        ScalingMode: "Flat",
        Weight: weight,
        ItemType: "Consumable",
        ItemRarity: "Uncommon",
        ItemEffectType: "Guard");

    private static Mock<ICatalogContentGateway> CreateGatewayWithItemPool(
        IReadOnlyCollection<CatalogRewardTemplateOptionSnapshot> options, int maxChoices = 3)
    {
        var template = new CatalogRewardTemplateSnapshot(
            Key: "reward.item.default",
            Version: "1.0.0",
            DisplayName: "Objets du Palais",
            Description: "desc",
            SourceType: "NodeEvent",
            MinChoices: maxChoices,
            MaxChoices: maxChoices,
            Options: options);

        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.GetRewardTemplateByKeyAsync("reward.item.default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogRewardTemplateSnapshot>.Success(template));

        return gateway;
    }

    [Fact]
    public async Task CreateItemRewardOfferAsync_ShouldSampleExactlyMaxChoices_FromTheCatalogPool()
    {
        var options = Enumerable.Range(1, 5).Select(i => CreateOption($"opt-{i}")).ToList();
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 3);

        var offer = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateItemRewardOfferAsync_ShouldBeDeterministic_ForTheSameSeedRunNodeAndRerollNonce()
    {
        var options = Enumerable.Range(1, 5).Select(i => CreateOption($"opt-{i}")).ToList();
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 3);
        var runId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var first = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle", runId, nodeId, rerollNonce: 0);
        var second = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle", runId, nodeId, rerollNonce: 0);

        first.Choices.Select(c => c.PayloadKey).Should().Equal(second.Choices.Select(c => c.PayloadKey));
    }

    [Fact]
    public async Task CreateItemRewardOfferAsync_ShouldDrawADifferentSubset_WhenRerollNonceChanges()
    {
        var options = Enumerable.Range(1, 8).Select(i => CreateOption($"opt-{i}")).ToList();
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 3);
        var runId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var initial = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle-reroll", runId, nodeId, rerollNonce: 0);
        var rerolled = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle-reroll", runId, nodeId, rerollNonce: 1);

        rerolled.Choices.Select(c => c.PayloadKey)
            .Should().NotBeEquivalentTo(initial.Choices.Select(c => c.PayloadKey),
            because: "a pool larger than the shown count should reroll into a different subset.");
    }

    [Fact]
    public async Task CreateItemRewardOfferAsync_ShouldThreadItemTypeRarityAndEffectType_IntoThePayloadKey()
    {
        var option = new CatalogRewardTemplateOptionSnapshot(
            RewardType: "TemporaryItem",
            Label: "Éclat de garde",
            Description: "Offre une protection.",
            PayloadKey: "item.consumable.guard-shard",
            PayloadType: "Item",
            EffectSetKey: null,
            BaseAmount: 8,
            ScalingMode: "Flat",
            Weight: 1,
            ItemType: "Consumable",
            ItemRarity: "Uncommon",
            ItemEffectType: "Guard");
        var gateway = CreateGatewayWithItemPool([option], maxChoices: 1);

        var offer = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, null, "seed-chandelle-typed", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().ContainSingle().Which.PayloadKey.Should().Be(
            "item:item.consumable.guard-shard:Éclat de garde:Offre une protection.:Consumable:Uncommon:Guard:8");
    }

    [Fact]
    public async Task CreateItemRewardOfferAsync_ShouldSampleFourChoices_WhenAbondanceIsActive_AgainstTheCatalogPool()
    {
        var options = Enumerable.Range(1, 6).Select(i => CreateOption($"opt-{i}")).ToList();
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 3);
        var modifier = RunModifier.Create(
            RunModifierType.AbondanceExtraChoiceEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-abondance-test");

        var offer = await CreateFactory(gateway.Object).CreateItemRewardOfferAsync(
            "default", riskLevel: 25, [modifier], "seed-chandelle-abondance", Guid.NewGuid(), Guid.NewGuid());

        offer.Choices.Should().HaveCount(4);
    }

    // -----------------------------------------------------------------------
    // Merchant purchase offer — real catalog items priced by rarity tier, always
    // with a free "Refuser" choice so the player is never forced into a purchase.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateMerchantRewardOfferAsync_ShouldPriceEachItemByItsRarity_AndIncludeAFreeDeclineChoice()
    {
        var options = new[]
        {
            CreateOption("common-item") with { ItemRarity = "Common" },
            CreateOption("epic-item") with { ItemRarity = "Epic" },
        };
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 2);

        var offer = await CreateFactory(gateway.Object).CreateMerchantRewardOfferAsync(
            riskLevel: 25, runSeed: "seed-merchant", runId: Guid.NewGuid(), nodeId: Guid.NewGuid());

        offer.Choices.Should().HaveCount(3, because: "2 sampled items + the always-free Refuser choice");

        var commonChoice = offer.Choices.Single(c => c.PayloadKey.Contains("common-item"));
        commonChoice.PalaceShardCost.Should().Be(150);
        commonChoice.HimLitShardCost.Should().Be(0);

        var epicChoice = offer.Choices.Single(c => c.PayloadKey.Contains("epic-item"));
        epicChoice.PalaceShardCost.Should().Be(500);
        epicChoice.HimLitShardCost.Should().Be(25);

        var declineChoice = offer.Choices.Single(c => c.RewardType == RewardType.Decline);
        declineChoice.PalaceShardCost.Should().Be(0);
        declineChoice.HimLitShardCost.Should().Be(0);
    }

    [Fact]
    public async Task CreateMerchantRewardOfferAsync_ShouldFallBackToFreeHardcodedChoices_WhenCatalogPoolIsUnavailable()
    {
        // CreateFactory() with no gateway hits the unconfigured Mock.Of<ICatalogContentGateway>()
        // default, mirroring how the item-node fallback is exercised above.
        var offer = await CreateFactory().CreateMerchantRewardOfferAsync(
            riskLevel: 25, runSeed: "seed-merchant-fallback", runId: Guid.NewGuid(), nodeId: Guid.NewGuid());

        offer.Choices.Should().Contain(c => c.RewardType == RewardType.Decline);
        offer.Choices.Where(c => c.RewardType != RewardType.Decline)
            .Should().OnlyContain(c => c.PalaceShardCost == 0 && c.HimLitShardCost == 0,
            because: "the hardcoded fallback pool predates real pricing and stays free.");
    }

    [Fact]
    public async Task CreateMerchantRewardOfferAsync_ShouldBeDeterministic_ForTheSameSeedRunAndNode()
    {
        var options = Enumerable.Range(1, 6).Select(i => CreateOption($"opt-{i}")).ToList();
        var gateway = CreateGatewayWithItemPool(options, maxChoices: 3);
        var runId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var first = await CreateFactory(gateway.Object).CreateMerchantRewardOfferAsync(
            riskLevel: 25, runSeed: "seed-merchant-det", runId: runId, nodeId: nodeId);
        var second = await CreateFactory(gateway.Object).CreateMerchantRewardOfferAsync(
            riskLevel: 25, runSeed: "seed-merchant-det", runId: runId, nodeId: nodeId);

        first.Choices.Select(c => c.PayloadKey).Should().Equal(second.Choices.Select(c => c.PayloadKey));
    }
}
