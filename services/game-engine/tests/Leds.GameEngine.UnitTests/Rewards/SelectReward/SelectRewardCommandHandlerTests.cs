using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards.SelectReward;

public sealed class SelectRewardCommandHandlerTests
{
    private static RewardOfferFactory CreateFactory() =>
        new(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));

    private static (Run run, RewardOffer offer) CreateRunWithPendingReward()
    {
        var run = TestGameEngineFactory.CreateRun();

        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: (int)RiskTier.Tendu);

        run.SetPendingRewardOffer(offer.Id);

        return (run, offer);
    }

    [Fact]
    public async Task Handle_ShouldSelectReward_WhenChoiceIsValid()
    {
        var (run, offer) = CreateRunWithPendingReward();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.PendingRewardOfferId.Should().BeNull();
        response.RewardOffer.State.Should().Be(RewardOfferState.Selected.ToString());
        response.RewardOffer.SelectedChoiceId.Should().Be(choiceId.Value);
    }

    [Fact]
    public async Task Handle_ShouldPersistRunAfterRewardApplication()
    {
        var (run, offer) = CreateRunWithPendingReward();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        runRepository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
        rewardRepository.Verify(
            repo => repo.UpdateAsync(offer, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldApplyHealEffect_WhenHealChoiceIsSelected()
    {
        var room = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-test-heal",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 20,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var healChoice = offer.Choices.First(c => c.RewardType == RewardType.Heal);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, healChoice.Id.Value),
            CancellationToken.None);

        // Tendu tier: baseHeal = (2-1)*5+10 = 15, DifficultyMultiplier = 1.15 ->
        // ceiling(15*1.15) = 18 healed, capped at MaxHp (20+18=38, well under 40).
        run.CurrentHp.Should().Be(38);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRunHasNoPendingReward()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Run has no pending reward offer.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenChoiceDoesNotBelongToOffer()
    {
        var (run, offer) = CreateRunWithPendingReward();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Reward choice was not found in the offer.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRewardOfferIsAlreadySelected()
    {
        var (run, offer) = CreateRunWithPendingReward();
        var choiceId = offer.Choices.First().Id;
        offer.SelectChoice(choiceId);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Only a pending reward offer can be selected.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var rewardRepository = new Mock<IRewardOfferRepository>();

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var act = () => handler.Handle(
            new SelectRewardCommand(runId, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task SelectReward_ShouldClearPendingRewardAndAllowProgression()
    {
        // Arrange: run with a combat node resolved and a pending reward offer
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(
            Leds.GameEngine.Domain.Nodes.NodeEventType.Combat);

        var run = runWithNode.Run;
        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(
            Leds.GameEngine.Domain.Rewards.RewardSource.Combat,
            NodeEventType.Combat,
            riskLevel: (int)RiskTier.Tendu);

        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var choiceId = offer.Choices.First().Id;

        // Act
        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        // Assert: reward cleared
        response.Run.PendingRewardOfferId.Should().BeNull(
            because: "PendingRewardOfferId must be null after selection.");

        // Assert: run is still in a state that allows ProgressRun
        run.HasActiveCombat.Should().BeFalse();
        run.HasPendingRewardOffer.Should().BeFalse(
            because: "HasPendingRewardOffer must be false — backend guard in ProgressRunCommandHandler.");
        run.Status.Should().Be(RunStatus.Active,
            because: "Run must still be Active after reward selection on a non-boss node.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRewardOfferDoesNotExist()
    {
        var run = TestGameEngineFactory.CreateRun();

        var fakeOfferId = RewardOfferId.New();
        run.SetPendingRewardOffer(fakeOfferId);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(fakeOfferId, CancellationToken.None))
            .ReturnsAsync((RewardOffer?)null);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"RewardOffer with id '{fakeOfferId.Value}' was not found.");
    }

    // ── Merchant purchase (choices with a real currency cost) ──────────────────

    private static (Run run, RewardOffer offer, RewardChoice purchaseChoice, RewardChoice declineChoice)
        CreateRunWithMerchantOffer(int palaceShardCost, int himLitShardCost)
    {
        var run = TestGameEngineFactory.CreateRun();

        var purchaseChoice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Baume de mémoire",
            "Restaure une partie de la vitalité.",
            "item:item.consumable.minor-heal:Baume de mémoire:Restaure une partie de la vitalité.:Consumable:Common:Heal:15",
            palaceShardCost: palaceShardCost,
            himLitShardCost: himLitShardCost);

        var declineChoice = RewardChoice.Create(
            RewardType.Decline,
            "Refuser",
            "Tu quittes le marchand les mains vides.",
            "decline:merchant");

        var offer = RewardOffer.Create(RewardSource.NodeEvent, [purchaseChoice, declineChoice]);
        run.SetPendingRewardOffer(offer.Id);

        return (run, offer, purchaseChoice, declineChoice);
    }

    private static SelectRewardCommandHandler CreateHandler(
        Run run, RewardOffer offer, StubPlayerProfileGateway playerProfileGateway)
    {
        var runRepository = new Mock<IRunRepository>();
        runRepository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository.Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None)).ReturnsAsync(offer);

        // Item enrichment (post-purchase) looks up the catalog definition; not the focus
        // of these tests, so make it a clean miss rather than an unconfigured-mock NRE.
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("not_found", "not found")));

        return new SelectRewardCommandHandler(
            runRepository.Object, rewardRepository.Object, catalogGateway.Object, playerProfileGateway);
    }

    [Fact]
    public async Task Handle_ShouldGrantItemAndDeductBothCurrencies_WhenPurchaseIsAffordable()
    {
        var (run, offer, purchaseChoice, _) = CreateRunWithMerchantOffer(palaceShardCost: 500, himLitShardCost: 25);

        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 500);
        playerProfileGateway.SeedHimLitCurrencyBalance(run.PlayerId, 25);

        var handler = CreateHandler(run, offer, playerProfileGateway);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, purchaseChoice.Id.Value),
            CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should().ContainSingle()
            .Which.Should().Be((run.PlayerId, 500, true));
        playerProfileGateway.SpentHimLitCurrencyAttempts.Should().ContainSingle()
            .Which.Should().Be((run.PlayerId, 25, true));
        run.RunItems.Should().ContainSingle(i => i.DefinitionKey == "item.consumable.minor-heal");
        offer.State.Should().Be(RewardOfferState.Selected);
        run.HasPendingRewardOffer.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldThrowAndLeaveOfferPending_WhenPalaceShardsAreInsufficient()
    {
        var (run, offer, purchaseChoice, _) = CreateRunWithMerchantOffer(palaceShardCost: 500, himLitShardCost: 25);

        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 100);
        playerProfileGateway.SeedHimLitCurrencyBalance(run.PlayerId, 25);

        var handler = CreateHandler(run, offer, playerProfileGateway);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, purchaseChoice.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Fonds insuffisants pour cet achat.");

        playerProfileGateway.SpentCurrencyAttempts.Should().BeEmpty(
            because: "the pre-check must reject before any spend is attempted");
        playerProfileGateway.SpentHimLitCurrencyAttempts.Should().BeEmpty();
        run.RunItems.Should().BeEmpty();
        offer.State.Should().Be(RewardOfferState.Pending,
            because: "a failed purchase must leave the offer selectable again");
        run.HasPendingRewardOffer.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefundPalaceShards_WhenHimLitSpendFailsDespitePassingPreCheck()
    {
        // The player-service balance read at pre-check time says the purchase is
        // affordable, but the actual Him'Lit spend call fails anyway (e.g. a concurrent
        // spend elsewhere, or any other transient player-service failure) — the Palace
        // shards already spent for this purchase must be handed back rather than lost.
        var (run, offer, purchaseChoice, _) = CreateRunWithMerchantOffer(palaceShardCost: 500, himLitShardCost: 25);

        var playerProfileGateway = new StubPlayerProfileGateway
        {
            ForceHimLitSpendFailure = true
        };
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 500);
        playerProfileGateway.SeedHimLitCurrencyBalance(run.PlayerId, 25);

        var handler = CreateHandler(run, offer, playerProfileGateway);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, purchaseChoice.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Fonds insuffisants pour cet achat.");

        playerProfileGateway.SpentCurrencyAttempts.Should().ContainSingle()
            .Which.Should().Be((run.PlayerId, 500, true));
        playerProfileGateway.SpentHimLitCurrencyAttempts.Should().ContainSingle()
            .Which.Should().Be((run.PlayerId, 25, false));
        playerProfileGateway.AwardedCurrency.Should().ContainSingle()
            .Which.Should().Be((run.PlayerId, 500), because: "the failed purchase must refund the Palace shards already spent");
        run.RunItems.Should().BeEmpty();
        offer.State.Should().Be(RewardOfferState.Pending);
    }

    [Fact]
    public async Task Handle_ShouldApplyDeclineAsNoOp_WithoutTouchingCurrency()
    {
        var (run, offer, _, declineChoice) = CreateRunWithMerchantOffer(palaceShardCost: 500, himLitShardCost: 25);

        var playerProfileGateway = new StubPlayerProfileGateway();
        // No balance seeded at all — declining must never touch the currency gateway.

        var handler = CreateHandler(run, offer, playerProfileGateway);

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, declineChoice.Id.Value),
            CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should().BeEmpty();
        playerProfileGateway.SpentHimLitCurrencyAttempts.Should().BeEmpty();
        run.RunItems.Should().BeEmpty();
        response.Run.PendingRewardOfferId.Should().BeNull();
        offer.State.Should().Be(RewardOfferState.Selected);
        offer.SelectedChoiceId.Should().Be(declineChoice.Id);
    }
}
