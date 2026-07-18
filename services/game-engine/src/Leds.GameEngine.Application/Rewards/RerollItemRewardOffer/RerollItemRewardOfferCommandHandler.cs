using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Rewards.RerollItemRewardOffer;

public sealed class RerollItemRewardOfferCommandHandler
    : IRequestHandler<RerollItemRewardOfferCommand, RerollItemRewardOfferResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory _rewardOfferFactory;

    public RerollItemRewardOfferCommandHandler(
        IRunRepository runRepository,
        IRewardOfferRepository rewardOfferRepository,
        Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory rewardOfferFactory)
    {
        _runRepository = runRepository;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
    }

    public async Task<RerollItemRewardOfferResponse> Handle(
        RerollItemRewardOfferCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        if (!run.HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        var rewardOffer = await _rewardOfferRepository.GetByIdAsync(
            run.PendingRewardOfferId!.Value, cancellationToken)
            ?? throw new NotFoundException("RewardOffer", run.PendingRewardOfferId!.Value);

        if (!rewardOffer.IsPending)
        {
            throw new DomainException("Only a pending reward offer can be rerolled.");
        }

        if (rewardOffer.Source != RewardSource.NodeEvent)
        {
            throw new DomainException("Only an item-node reward offer can be rerolled.");
        }

        if (!run.TryConsumeItemNodeRerollCharge())
        {
            throw new DomainException("No reroll charges available.");
        }

        // rewardProfile/riskLevel only matter for the hardcoded fallback pool (see
        // RewardOfferFactory.CreateItemRewardOfferAsync) — the offer being rerolled
        // doesn't retain the originating node's values, and the catalog-driven pool
        // (the normal path) doesn't need them at all.
        var rerolled = await _rewardOfferFactory.CreateItemRewardOfferAsync(
            rewardProfile: "default",
            riskLevel: 0,
            run.RunModifiers,
            run.Seed,
            run.Id.Value,
            rewardOffer.Id.Value,
            rerollNonce: run.ConsumedItemNodeRerollCount,
            cancellationToken);

        rewardOffer.ReplaceChoices(rerolled.Choices);

        await _rewardOfferRepository.UpdateAsync(rewardOffer, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new RerollItemRewardOfferResponse(
            RunDto.FromDomain(run),
            RewardOfferDto.FromDomain(rewardOffer));
    }
}
