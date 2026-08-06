using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards.SelectReward;

public sealed class SelectRewardItemEnrichmentTests
{
    private static RewardOfferFactory CreateFactory() =>
        new(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));

    [Fact]
    public async Task Handle_ShouldEnrichItemWithCatalog_WhenItemRewardSelected()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-enrich-test",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(p => p.GetItemDefinitionByKeyAsync("item.consumable.minor-heal", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.consumable.minor-heal", "1.0", "Baume", "Soin", null,
                "Consumable", "Heal", "Common", "UseInCombat", "RuntimeRunOnly",
                "Additive", 99, true, true, null)));

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var itemChoice = offer.Choices.First(c => c.RewardType == RewardType.TemporaryItem);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, itemChoice.Id.Value),
            default);

        var addedItem = run.RunItems.Last();
        addedItem.DefinitionVersion.Should().Be("1.0");
        addedItem.Category.Should().Be("Consumable");
        addedItem.UsageMode.Should().Be("UseInCombat");
        addedItem.Lifecycle.Should().Be("RuntimeRunOnly");
        addedItem.MaxStack.Should().Be(99);
        addedItem.SourceRewardOptionId.Should().Be(itemChoice.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldAppendJournalEntry_WhenItemRewardSelected_AndJournalEnabled()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-enrich-journal",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            journalEnabled: true,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(p => p.GetItemDefinitionByKeyAsync("item.consumable.minor-heal", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.consumable.minor-heal", "1.0", "Baume", "Soin", null,
                "Consumable", "Heal", "Common", "UseInCombat", "RuntimeRunOnly",
                "Additive", 99, true, true, null)));

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var itemChoice = offer.Choices.First(c => c.RewardType == RewardType.TemporaryItem);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, itemChoice.Id.Value),
            default);

        run.JournalEntries.Should().ContainSingle(entry => entry.Text.Contains("Baume"));
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenItemDefinitionNotFound()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-no-def",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(p => p.GetItemDefinitionByKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("catalog.item_definition_not_found", "not found")));

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var itemChoice = offer.Choices.First(c => c.RewardType == RewardType.TemporaryItem);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, itemChoice.Id.Value),
            default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ShouldNotEnrichHealChoice_WhenHealSelected()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-heal-only",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            currentHp: 20,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var healChoice = offer.Choices.First(c => c.RewardType == RewardType.Heal);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, healChoice.Id.Value),
            default);

        await act.Should().NotThrowAsync();
        catalogGateway.Verify(
            p => p.GetItemDefinitionByKeyAsync(It.IsAny<string>(), default),
            Times.Never);
    }

    // Regression: the reward payload carries no tactical fields, so every granted item used to
    // fall back to TacticalRange 1 — usable only on an adjacent ally, which made "heal a
    // teammate who isn't standing next to me" impossible in practice (see RunItem.Create).
    [Fact]
    public async Task Handle_ShouldGrantAUsableTacticalRange_ForASingleAllyTargetedItem()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-tactical-range",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(p => p.GetItemDefinitionByKeyAsync("item.consumable.minor-heal", default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.consumable.minor-heal", "1.0", "Baume", "Soin", null,
                "Consumable", "Heal", "Common", "UseInCombat", "RuntimeRunOnly",
                "Additive", 99, true, true, null,
                // Matches CatalogSeedRunner.UpsertItemAsync's now-corrected default for a
                // usable-in-combat consumable — see the "Handle_ShouldNotEnrich..." tests
                // above for the case where the catalog lookup fails and RunItem.Create's own
                // fallback default (also bumped) is what actually reaches the player.
                TacticalRange: 4)));

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var itemChoice = offer.Choices.First(c => c.RewardType == RewardType.TemporaryItem);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, itemChoice.Id.Value),
            default);

        var addedItem = run.RunItems.Last();
        addedItem.BattleTargetingType.Should().Be("SingleAlly");
        addedItem.TacticalRange.Should().BeGreaterThan(1);
    }

    // Regression: items granted through "reward.item.default" (item nodes, merchant) have no
    // matching catalog ItemDefinition row at all — GetItemDefinitionByKeyAsync always fails for
    // them, so enrichment never runs and RunItem.Create's own fallback default is what actually
    // reaches the player. That fallback used to be TacticalRange 1 (see RunItem.Create).
    [Fact]
    public async Task Handle_ShouldStillGrantAUsableTacticalRange_WhenCatalogEnrichmentFails()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-tactical-range-no-catalog",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(p => p.GetItemDefinitionByKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("catalog.item_definition_not_found", "not found")));

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object, Mock.Of<IPlayerProfileGateway>());

        var itemChoice = offer.Choices.First(c => c.RewardType == RewardType.TemporaryItem);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, itemChoice.Id.Value),
            default);

        var addedItem = run.RunItems.Last();
        addedItem.TacticalRange.Should().BeGreaterThan(1);
    }
}
