using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
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
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public SelectRewardCommandHandler(
        IRunRepository runRepository,
        IRewardOfferRepository rewardOfferRepository,
        ICatalogContentGateway catalogContentGateway,
        IPlayerProfileGateway playerProfileGateway)
    {
        _runRepository = runRepository;
        _rewardOfferRepository = rewardOfferRepository;
        _catalogContentGateway = catalogContentGateway;
        _playerProfileGateway = playerProfileGateway;
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

        // Merchant purchase: this choice has a real cost (in one or both Palace
        // currencies) — check affordability and actually spend before granting
        // anything. Every other reward source (item nodes, combat loot, rest heal
        // choices) leaves both costs at zero and skips this branch entirely.
        if (selectedChoice.PalaceShardCost > 0 || selectedChoice.HimLitShardCost > 0)
        {
            await SpendForPurchaseAsync(run.PlayerId, selectedChoice, cancellationToken);
        }

        // Item rewards are resolved against the catalog up front — both to enrich the
        // granted RunItem with display data (as before) and, per the "modèle Hadès",
        // to decide whether the item ever touches the run's temporary inventory at
        // all: a permanent-eligible item (CatalogItemDefinitionSnapshot.IsPermanentEligible
        // — weapons, relics, anything meant to be owned for good) joins the player's
        // permanent backpack immediately instead of waiting in the Besace for an
        // end-of-run keepsake confirmation. Only consumables still take the old path.
        CatalogItemDefinitionSnapshot? itemDefinition = null;
        if (selectedChoice.RewardType == RewardType.TemporaryItem)
        {
            var defKey = ParseItemDefinitionKey(selectedChoice.PayloadKey);
            if (defKey is not null)
            {
                var definitionResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(defKey, cancellationToken);
                if (definitionResult.IsSuccess)
                {
                    itemDefinition = definitionResult.Value;
                }
            }
        }

        if (itemDefinition is { IsPermanentEligible: true })
        {
            await _playerProfileGateway.AddPermanentItemsAsync(
                run.PlayerId, [itemDefinition.Key], run.Id.Value, cancellationToken);

            run.AppendJournalEntry(RunJournalNarrator.DescribeItemFound(
                run.CurrentRoom.CatalogBinding?.DisplayName, itemDefinition.DisplayName));
        }
        else
        {
            // Validate + apply the reward effect before marking the offer as selected,
            // so a failed ApplyRewardEffect does not corrupt the in-memory offer state.
            run.ApplyReward(selectedChoice);

            if (itemDefinition is not null)
            {
                run.EnrichLastAddedItem(
                    definitionVersion: itemDefinition.Version,
                    narrativeText: itemDefinition.NarrativeText,
                    category: itemDefinition.Category,
                    usageMode: itemDefinition.UsageMode,
                    lifecycle: itemDefinition.Lifecycle,
                    maxStack: itemDefinition.MaxStack,
                    isUsableInCombat: itemDefinition.IsUsableInCombat,
                    isUsableOutsideCombat: itemDefinition.IsUsableOutsideCombat,
                    sourceRewardOptionId: selectedChoice.Id.Value,
                    isContainer: itemDefinition.IsContainer,
                    containerCapacity: itemDefinition.ContainerCapacity,
                    isLiquid: itemDefinition.IsLiquid,
                    tacticalRange: itemDefinition.TacticalRange,
                    tacticalAreaShape: itemDefinition.TacticalAreaShape,
                    requiresLineOfSight: itemDefinition.RequiresLineOfSight);

                run.AppendJournalEntry(RunJournalNarrator.DescribeItemFound(
                    run.CurrentRoom.CatalogBinding?.DisplayName, itemDefinition.DisplayName));
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

    /// <summary>
    /// Checks the player can afford both currency costs of a merchant purchase, then
    /// actually deducts them. Reads the balance up front so an unaffordable purchase
    /// fails cleanly (no spend attempted, offer stays pending — the player can pick a
    /// cheaper item or "Refuser") instead of leaving a half-paid transaction. The two
    /// TrySpend calls are not atomic with each other (separate player-service round
    /// trips), so if the Him'Lit leg fails despite the pre-check — a concurrent spend
    /// elsewhere, most plausibly — the Palace shards already spent are refunded and the
    /// purchase is rejected rather than silently granting a partially-paid item.
    /// </summary>
    private async Task SpendForPurchaseAsync(
        Guid playerId, RewardChoice choice, CancellationToken cancellationToken)
    {
        var profile = await _playerProfileGateway.GetProfileAsync(playerId, cancellationToken);

        if (profile.Progression.PalaceShardCount < choice.PalaceShardCost
            || profile.Progression.HimLitShardCount < choice.HimLitShardCost)
        {
            throw new DomainException("Fonds insuffisants pour cet achat.");
        }

        if (choice.PalaceShardCost > 0)
        {
            var paidPalace = await _playerProfileGateway.TrySpendCurrencyAsync(
                playerId, choice.PalaceShardCost, cancellationToken);

            if (!paidPalace)
            {
                throw new DomainException("Fonds insuffisants pour cet achat.");
            }
        }

        if (choice.HimLitShardCost > 0)
        {
            var paidHimLit = await _playerProfileGateway.TrySpendHimLitCurrencyAsync(
                playerId, choice.HimLitShardCost, cancellationToken);

            if (!paidHimLit)
            {
                if (choice.PalaceShardCost > 0)
                {
                    await _playerProfileGateway.AwardCurrencyAsync(
                        playerId, choice.PalaceShardCost, cancellationToken);
                }

                throw new DomainException("Fonds insuffisants pour cet achat.");
            }
        }
    }
}
