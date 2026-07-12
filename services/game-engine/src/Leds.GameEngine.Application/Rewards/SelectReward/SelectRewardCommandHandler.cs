using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs;
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
    private readonly ICatalogContentGateway _catalogContentGateway;

    public SelectRewardCommandHandler(
        IRunRepository runRepository,
        IRewardOfferRepository rewardOfferRepository,
        ICatalogContentGateway catalogContentGateway)
    {
        _runRepository = runRepository;
        _rewardOfferRepository = rewardOfferRepository;
        _catalogContentGateway = catalogContentGateway;
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

        // Enrich items with catalog snapshot data when an item reward is selected.
        if (selectedChoice.RewardType == RewardType.TemporaryItem)
        {
            var defKey = ParseItemDefinitionKey(selectedChoice.PayloadKey);
            if (defKey is not null)
            {
                var definitionResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(defKey, cancellationToken);
                if (definitionResult.IsSuccess)
                {
                    var def = definitionResult.Value;
                    run.EnrichLastAddedItem(
                        definitionVersion: def.Version,
                        narrativeText: def.NarrativeText,
                        category: def.Category,
                        usageMode: def.UsageMode,
                        lifecycle: def.Lifecycle,
                        maxStack: def.MaxStack,
                        effectSetKey: def.EffectSetKey,
                        isUsableInCombat: def.IsUsableInCombat,
                        isUsableOutsideCombat: def.IsUsableOutsideCombat,
                        sourceRewardOptionId: selectedChoice.Id.Value,
                        isContainer: def.IsContainer,
                        containerCapacity: def.ContainerCapacity,
                        isLiquid: def.IsLiquid);

                    run.AppendJournalEntry(RunJournalNarrator.DescribeItemFound(
                        run.CurrentRoom.CatalogBinding?.DisplayName, def.DisplayName));
                }
            }
        }

        rewardOffer.SelectChoice(choiceId);

        run.ClearPendingRewardOffer();

        await _rewardOfferRepository.UpdateAsync(rewardOffer, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SelectRewardResponse(
            RunDto.FromDomain(run),
            RewardOfferDto.FromDomain(rewardOffer));
    }

    private static string? ParseItemDefinitionKey(string payloadKey)
    {
        // Payload format: "item:<definitionKey>:..."
        var parts = payloadKey.Split(':', StringSplitOptions.TrimEntries);
        return parts.Length >= 2 && string.Equals(parts[0], "item", StringComparison.OrdinalIgnoreCase)
            ? parts[1]
            : null;
    }
}
