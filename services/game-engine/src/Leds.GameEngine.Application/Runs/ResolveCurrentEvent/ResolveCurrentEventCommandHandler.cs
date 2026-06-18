using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandler
    : IRequestHandler<ResolveCurrentEventCommand, ResolveCurrentEventResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly INodeEventResolverDispatcher _nodeEventResolverDispatcher;
    private readonly IEventContentResolver _eventContentResolver;
    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly ICombatInstanceFactory _combatInstanceFactory;
    private readonly ICombatInstanceRepository _combatInstanceRepository;
    private readonly ICombatEncounterDraftGenerator _encounterDraftGenerator;
    private readonly ICombatFactory _combatFactory;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory _rewardOfferFactory;

    public ResolveCurrentEventCommandHandler(
        IRunRepository runRepository,
        INodeEventResolverDispatcher nodeEventResolverDispatcher,
        IEventContentResolver eventContentResolver,
        ICatalogContentGateway catalogContentGateway,
        ICombatInstanceFactory combatInstanceFactory,
        ICombatInstanceRepository combatInstanceRepository,
        ICombatEncounterDraftGenerator encounterDraftGenerator,
        ICombatFactory combatFactory,
        IRewardOfferRepository rewardOfferRepository,
        Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory rewardOfferFactory)
    {
        _runRepository = runRepository;
        _nodeEventResolverDispatcher = nodeEventResolverDispatcher;
        _eventContentResolver = eventContentResolver;
        _catalogContentGateway = catalogContentGateway;
        _combatInstanceFactory = combatInstanceFactory;
        _combatInstanceRepository = combatInstanceRepository;
        _encounterDraftGenerator = encounterDraftGenerator;
        _combatFactory = combatFactory;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
    }

    public async Task<ResolveCurrentEventResponse> Handle(
        ResolveCurrentEventCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status == RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot resolve an event: run is in Interlude. Navigate the interlude hub or enter the next room.");
        }

        var room = run.CurrentRoom;
        var selectedNode = room.Nodes.SingleOrDefault(node =>
            node.Row == room.CurrentNodeDepth &&
            node.State == NodeState.Selected);

        if (selectedNode is null)
        {
            throw new DomainException("No node has been selected for the current room depth.");
        }

        var resolutionContext = new NodeEventResolutionContext(
            run,
            room,
            selectedNode);

        var resolutionResult = _nodeEventResolverDispatcher.Resolve(resolutionContext);

        var isCombat = resolutionResult.ResolutionKind is NodeEventResolutionKind.CombatStarted
            or NodeEventResolutionKind.EliteEncounterStarted
            or NodeEventResolutionKind.RoomBossEncounterStarted
            or NodeEventResolutionKind.RareCombatStarted;

        CombatEncounterDraftDto? encounterDraftDto = null;
        CombatRuntimeDto? combatRuntimeDto = null;
        ResolvedNodeEventContent? resolvedContent = null;

        if (isCombat)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);

            var (enemyTemplateKey, _) = resolvedContent switch
            {
                ResolvedCombatEventContent c => (c.EnemyTemplateKey, c.RiskLevel),
                ResolvedEliteEventContent e => (e.EnemyTemplateKey, e.RiskLevel),
                ResolvedRoomBossEventContent b => (b.EnemyTemplateKey, b.RiskLevel),
                ResolvedRareCombatEventContent r => (r.EnemyTemplateKey, r.RiskLevel),
                _ => throw new DomainException(
                    "Expected combat, elite, room boss, or rare combat event content but got a different type.")
            };

            var enemyTemplateResult = await _catalogContentGateway.GetEnemyTemplateByKeyAsync(
                enemyTemplateKey, cancellationToken);

            if (enemyTemplateResult.IsFailure)
            {
                throw new DomainException(
                    $"Failed to retrieve enemy template: {enemyTemplateResult.Error.Message}");
            }

            var combatInstance = _combatInstanceFactory.CreateFromEnemyTemplate(
                enemyTemplateResult.Value);

            await _combatInstanceRepository.AddAsync(combatInstance, cancellationToken);

            run.SetActiveCombat(combatInstance.Id);

            var draft = await GenerateEncounterDraft(
                run, room, selectedNode, resolutionResult, cancellationToken);

            // Apply combat difficulty modifiers from curses and Palace Laws.
            var difficultyModifiers = run.GetActiveModifiers(RunModifierType.NextCombatDifficultyMultiplier)
                .Concat(run.GetActiveModifiers(RunModifierType.CombatDifficultyMultiplier))
                .Concat(run.GetActiveModifiers(RunModifierType.PermanentCombatDifficultyBonus))
                .ToArray();
            if (difficultyModifiers.Length > 0)
            {
                var additionalMultiplier = difficultyModifiers.Sum(m => m.Value);
                draft = draft with
                {
                    DifficultyMultiplier = Math.Min(draft.DifficultyMultiplier * (1.0 + additionalMultiplier), 2.0)
                };
            }

            encounterDraftDto = CombatEncounterDraftDto.FromDomain(draft);
            var combatRuntime = _combatFactory.CreateFromDraft(
                combatInstance.Id,
                draft,
                run.PlayerState,
                run.RunModifiers,
                attackPower: run.Attack,
                defense: run.Defense,
                speed: run.Speed,
                palaceRoomState: room.PalaceState);
            run.StartCombat(combatRuntime);
            combatRuntimeDto = CombatRuntimeDto.FromDomain(combatRuntime, CombatItemHelper.GetUsableBattleItems(run));
        }
        else if (selectedNode.EventType == NodeEventType.Item)
        {
            var itemRewardOffer = _rewardOfferFactory.CreateItemRewardOffer(
                selectedNode.RewardProfile,
                selectedNode.RiskLevel);

            await _rewardOfferRepository.AddAsync(itemRewardOffer, cancellationToken);
            run.SetPendingRewardOffer(itemRewardOffer.Id);
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("item");
        }
        else if (selectedNode.EventType == NodeEventType.Merchant)
        {
            var merchantRewardOffer = _rewardOfferFactory.CreateMerchantRewardOffer(
                selectedNode.RiskLevel);

            await _rewardOfferRepository.AddAsync(merchantRewardOffer, cancellationToken);
            run.SetPendingRewardOffer(merchantRewardOffer.Id);
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("trade");
        }
        else if (selectedNode.EventType == NodeEventType.Law)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);
            resolutionResult = ApplyPalaceLawContent(resolutionResult, (ResolvedPalaceLawEventContent)resolvedContent);
            run.ResolveCurrentEvent();
        }
        else
        {
            run.ResolveCurrentEvent();
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        var outcome = ResolvedNodeEventOutcomeDto.FromResult(
            selectedNode,
            resolutionResult);

        return new ResolveCurrentEventResponse(
            RunDto.FromDomain(run),
            outcome,
            encounterDraftDto,
            combatRuntimeDto);
    }

    private async Task<CombatEncounterDraft> GenerateEncounterDraft(
        Run run,
        Room room,
        MapNode selectedNode,
        NodeEventResolutionResult resolutionResult,
        CancellationToken cancellationToken)
    {
        var encounterType = resolutionResult.ResolutionKind switch
        {
            NodeEventResolutionKind.CombatStarted => "Combat",
            NodeEventResolutionKind.EliteEncounterStarted => "Elite",
            NodeEventResolutionKind.RoomBossEncounterStarted => "RoomBoss",
            NodeEventResolutionKind.RareCombatStarted => "Rare",
            _ => "Combat"
        };

        var catalogRiskLevel = Math.Clamp(selectedNode.RiskLevel / 20 + 1, 1, 5);

        var enemyCount = encounterType switch
        {
            "Elite" => 1,
            "Rare" => 1,
            "RoomBoss" => 1,
            _ => catalogRiskLevel >= 3 ? 2 : 1
        };

        var draftContext = new CombatEncounterDraftContext(
            RunId: run.Id.Value,
            RoomId: room.Id.Value,
            NodeId: selectedNode.Id.Value,
            RoomType: room.RoomType.ToString(),
            RoomIndex: room.Depth,
            RiskLevel: catalogRiskLevel,
            EncounterType: encounterType,
            EnemyCount: enemyCount,
            NodeDepth: selectedNode.Row,
            ActivePalaceLaws: run.ActivePalaceLaws,
            PalaceRoomState: room.PalaceState,
            RoomClimate: ResolveActiveClimate(run, room));

        return await _encounterDraftGenerator.GenerateAsync(
            draftContext, cancellationToken);
    }

    private async Task<ResolvedNodeEventContent> ResolveEventContentAsync(
        Run run,
        Room room,
        MapNode selectedNode,
        CancellationToken cancellationToken)
    {
        var contentContext = new EventContentResolutionContext(
            Seed: run.Seed,
            RoomType: room.RoomType,
            RoomDepth: room.Depth,
            NodeDepth: selectedNode.Row,
            EventOrder: 1,
            EventType: selectedNode.EventType,
            RiskLevel: selectedNode.RiskLevel,
            RewardProfile: selectedNode.RewardProfile,
            ActivePalaceLawKeys: run.ActivePalaceLaws
                .Where(law => !law.IsConsumed)
                .Select(law => law.Key)
                .ToArray());

        var contentResult = await _eventContentResolver.ResolveAsync(
            contentContext, cancellationToken);

        if (contentResult.IsFailure)
        {
            throw new DomainException(
                $"Failed to resolve event content: {contentResult.Error.Message}");
        }

        return contentResult.Value;
    }

    private static NodeEventResolutionResult ApplyPalaceLawContent(
        NodeEventResolutionResult result,
        ResolvedPalaceLawEventContent content)
    {
        return result with
        {
            Title = content.PalaceLawName,
            Description = content.PalaceLawDescription,
            Choices = result.Choices.Select(choice => choice.ChoiceId switch
            {
                "accept-law" => choice with { ChoiceId = $"accept-law:{content.PalaceLawDefinitionKey}" },
                _ => choice
            }).ToArray()
        };
    }

    private static string? ResolveActiveClimate(Run run, Room room)
    {
        var modifier = run.RunModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == room.Id.Value)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => "Grey",
            2 => "Rain",
            3 => "Heatwave",
            4 => "Hail",
            _ => null
        };
    }
}
