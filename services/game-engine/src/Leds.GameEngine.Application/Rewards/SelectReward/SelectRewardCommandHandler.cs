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

        var choiceId = new RewardChoiceId(request.ChoiceId);

        rewardOffer.SelectChoice(choiceId);

        run.ApplyRewardEffect(
            rewardOffer.Choices.Single(choice => choice.Id == choiceId));

        run.ClearPendingRewardOffer();

        await _rewardOfferRepository.UpdateAsync(rewardOffer, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SelectRewardResponse(
            RunDto.FromDomain(run),
            RewardOfferDto.FromDomain(rewardOffer));
    }
}
