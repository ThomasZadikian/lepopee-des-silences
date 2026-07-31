using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
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
            startedAt: DateTimeOffset.UtcNow);
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
            runRepo.Object, rewardRepo.Object, catalogGateway.Object);

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
            journalEnabled: true);
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
            runRepo.Object, rewardRepo.Object, catalogGateway.Object);

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
            startedAt: DateTimeOffset.UtcNow);
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
            runRepo.Object, rewardRepo.Object, catalogGateway.Object);

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
            currentHp: 20);
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, Domain.Nodes.NodeEventType.Combat, (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, default)).ReturnsAsync(run);

        var rewardRepo = new Mock<IRewardOfferRepository>();
        rewardRepo.Setup(r => r.GetByIdAsync(offer.Id, default)).ReturnsAsync(offer);

        var catalogGateway = new Mock<ICatalogContentGateway>();

        var handler = new SelectRewardCommandHandler(
            runRepo.Object, rewardRepo.Object, catalogGateway.Object);

        var healChoice = offer.Choices.First(c => c.RewardType == RewardType.Heal);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, healChoice.Id.Value),
            default);

        await act.Should().NotThrowAsync();
        catalogGateway.Verify(
            p => p.GetItemDefinitionByKeyAsync(It.IsAny<string>(), default),
            Times.Never);
    }
}
