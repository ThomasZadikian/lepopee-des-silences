using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Rewards.SelectReward;

public sealed class SelectRewardCommandHandlerTests
{
    private static RewardOfferFactory CreateFactory()
    {
        return new RewardOfferFactory();
    }

    private static (Run run, RewardOffer offer) CreateRunWithPendingReward()
    {
        var run = TestGameEngineFactory.CreateRun();

        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, riskLevel: 25);

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
            rewardRepository.Object);

        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.PendingRewardOfferId.Should().BeNull();
        response.RewardOffer.State.Should().Be(RewardOfferState.Selected.ToString());
        response.RewardOffer.SelectedChoiceId.Should().Be(choiceId.Value);
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
            currentHp: 20);

        var factory = CreateFactory();
        var offer = factory.CreateCombatRewardOffer(RewardSource.Combat, riskLevel: 25);
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
            rewardRepository.Object);

        var healChoice = offer.Choices.First(c => c.RewardType == RewardType.Heal);

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, healChoice.Id.Value),
            CancellationToken.None);

        run.CurrentHp.Should().Be(35);
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
            rewardRepository.Object);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Run has no pending reward offer.");
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
            rewardRepository.Object);

        var act = () => handler.Handle(
            new SelectRewardCommand(runId, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
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
            rewardRepository.Object);

        var act = () => handler.Handle(
            new SelectRewardCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"RewardOffer with id '{fakeOfferId.Value}' was not found.");
    }
}
