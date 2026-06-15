using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Rewards.SelectReward;

public sealed class SelectRewardCommandHandler
    : IRequestHandler<SelectRewardCommand, SelectRewardResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRewardOfferRepository _rewardOfferRepository;

    public SelectRewardCommandHandler(
        IRunRepository runRepository,
        IRewardOfferRepository rewardOfferRepository)
    {
        _runRepository = runRepository;
        _rewardOfferRepository = rewardOfferRepository;
    }

    public async Task<SelectRewardResponse> Handle(
        SelectRewardCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (!run.HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        var rewardOffer = await _rewardOfferRepository.GetByIdAsync(
            run.PendingRewardOfferId!.Value,
            cancellationToken);

        if (rewardOffer is null)
        {
            throw new NotFoundException("RewardOffer", run.PendingRewardOfferId!.Value);
        }

        if (!rewardOffer.IsPending)
        {
            throw new DomainException("Only a pending reward offer can be selected.");
        }

        var choiceId = new RewardChoiceId(request.ChoiceId);

        // Resolve the choice first (validates it exists in the offer) before mutating state.
        var selectedChoice = rewardOffer.Choices.SingleOrDefault(choice => choice.Id == choiceId)
            ?? throw new DomainException("Reward choice was not found in the offer.");

        // Validate + apply the reward effect before marking the offer as selected,
        // so a failed ApplyRewardEffect does not corrupt the in-memory offer state.
        run.ApplyReward(selectedChoice);

        rewardOffer.SelectChoice(choiceId);

        run.ClearPendingRewardOffer();

        await _rewardOfferRepository.UpdateAsync(rewardOffer, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SelectRewardResponse(
            RunDto.FromDomain(run),
            RewardOfferDto.FromDomain(rewardOffer));
    }
}