using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RerollItemRewardOffer;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards.RerollItemRewardOffer;

/// <summary>
/// "Loi de la Chandelle" (law.chandelle) — rerolls the pending item-node reward offer,
/// consuming one free reroll charge (RunModifierType.ItemNodeRerollCharge).
/// </summary>
public sealed class RerollItemRewardOfferCommandHandlerTests
{
    private static Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory CreateFactory() =>
        new(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));

    private static PalaceLaw CreateChandelleLaw() => PalaceLaw.Create(
        "law.chandelle", "Loi de la Chandelle", "1.0.0",
        domains: [PalaceLawDomain.Rewards],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.ItemNodeRerollCharge, value: 1, RunModifierDuration.UntilFloorEnds),
        ]);

    private static async Task<(Run run, RewardOffer offer)> CreateRunWithPendingItemRewardAsync(bool withChandelleCharge = true)
    {
        var run = TestGameEngineFactory.CreateRun();

        if (withChandelleCharge)
        {
            run.PromulgateLaw(CreateChandelleLaw());
        }

        var offer = await CreateFactory().CreateItemRewardOfferAsync(
            "default", riskLevel: 25, run.RunModifiers, run.Seed, run.Id.Value, Guid.NewGuid());
        run.SetPendingRewardOffer(offer.Id);

        return (run, offer);
    }

    private static (Mock<IRunRepository> runRepository, Mock<IRewardOfferRepository> rewardRepository) CreateRepositories(
        Run run, RewardOffer offer)
    {
        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        return (runRepository, rewardRepository);
    }

    [Fact]
    public async Task Handle_ShouldConsumeOneChargeAndReplaceTheChoices_WhenAChargeIsAvailable()
    {
        var (run, offer) = await CreateRunWithPendingItemRewardAsync();
        var (runRepository, rewardRepository) = CreateRepositories(run, offer);

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var response = await handler.Handle(
            new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        run.ConsumedItemNodeRerollCount.Should().Be(1);
        response.RewardOffer.Choices.Should().NotBeEmpty();
        offer.State.Should().Be(RewardOfferState.Pending,
            because: "a reroll swaps the choice set but the offer stays pending until selected.");
    }

    [Fact]
    public async Task Handle_ShouldPersistBothTheRunAndTheRewardOffer()
    {
        var (run, offer) = await CreateRunWithPendingItemRewardAsync();
        var (runRepository, rewardRepository) = CreateRepositories(run, offer);

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        await handler.Handle(new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        runRepository.Verify(repo => repo.UpdateAsync(run, CancellationToken.None), Times.Once);
        rewardRepository.Verify(repo => repo.UpdateAsync(offer, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRunHasNoPendingRewardOffer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateChandelleLaw());

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);
        var rewardRepository = new Mock<IRewardOfferRepository>();

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var act = () => handler.Handle(new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Run has no pending reward offer.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTheOfferIsNotAnItemNodeOffer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(CreateChandelleLaw());

        var combatOffer = CreateFactory().CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 25);
        run.SetPendingRewardOffer(combatOffer.Id);

        var (runRepository, rewardRepository) = CreateRepositories(run, combatOffer);

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var act = () => handler.Handle(new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Only an item-node reward offer can be rerolled.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTheOfferIsAlreadySelected()
    {
        var (run, offer) = await CreateRunWithPendingItemRewardAsync();
        offer.SelectChoice(offer.Choices.First().Id);

        var (runRepository, rewardRepository) = CreateRepositories(run, offer);

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var act = () => handler.Handle(new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Only a pending reward offer can be rerolled.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNoRerollChargeIsAvailable()
    {
        var (run, offer) = await CreateRunWithPendingItemRewardAsync(withChandelleCharge: false);
        var (runRepository, rewardRepository) = CreateRepositories(run, offer);

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var act = () => handler.Handle(new RerollItemRewardOfferCommand(run.Id.Value), CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No reroll charges available.");
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

        var handler = new RerollItemRewardOfferCommandHandler(
            runRepository.Object, rewardRepository.Object, CreateFactory());

        var act = () => handler.Handle(new RerollItemRewardOfferCommand(runId), CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }
}
