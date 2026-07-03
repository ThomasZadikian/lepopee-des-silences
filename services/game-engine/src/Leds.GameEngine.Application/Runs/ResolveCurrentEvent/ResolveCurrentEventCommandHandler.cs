using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Atb;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
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
    private readonly ICombatEncounterDraftGenerator _encounterDraftGenerator;
    private readonly ICombatFactory _combatFactory;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory _rewardOfferFactory;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IAtbCombatPreparer _atbPreparer;
    private readonly IClock _clock;

    public ResolveCurrentEventCommandHandler(
        IRunRepository runRepository,
        INodeEventResolverDispatcher nodeEventResolverDispatcher,
        IEventContentResolver eventContentResolver,
        ICatalogContentGateway catalogContentGateway,
        ICombatInstanceFactory combatInstanceFactory,
        ICombatEncounterDraftGenerator encounterDraftGenerator,
        ICombatFactory combatFactory,
        IRewardOfferRepository rewardOfferRepository,
        Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory rewardOfferFactory,
        IEnemyCombatTurnResolver enemyTurnResolver,
        ICombatResolutionService combatResolution,
        IAtbCombatPreparer atbPreparer,
        IClock clock)
    {
        _runRepository = runRepository;
        _nodeEventResolverDispatcher = nodeEventResolverDispatcher;
        _eventContentResolver = eventContentResolver;
        _catalogContentGateway = catalogContentGateway;
        _combatInstanceFactory = combatInstanceFactory;
        _encounterDraftGenerator = encounterDraftGenerator;
        _combatFactory = combatFactory;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
        _enemyTurnResolver = enemyTurnResolver;
        _combatResolution = combatResolution;
        _atbPreparer = atbPreparer;
        _clock = clock;
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
        RewardOffer? pendingRewardOffer = null;
        NpcDialogueViewDto? npcDialogue = null;

        if (isCombat)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);

            // Combat/Elite/RoomBoss/RareCombat content carries a legacy EnemyTemplateKey that
            // used to seed a throwaway single-enemy CombatInstance just to mint a CombatId —
            // the real encounter (enemies, stats, skills) always comes from the draft generated
            // below via ListCompatibleEnemyDefinitionsAsync, so that legacy catalog round-trip
            // was pure overhead (and broke entirely once the legacy EnemyTemplate content
            // stopped being seeded). Mint the id directly instead.
            if (resolvedContent is not (ResolvedCombatEventContent or ResolvedEliteEventContent
                or ResolvedRoomBossEventContent or ResolvedRareCombatEventContent))
            {
                throw new DomainException(
                    "Expected combat, elite, room boss, or rare combat event content but got a different type.");
            }

            var combatId = CombatId.New();

            run.SetActiveCombat(combatId);

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
                    DifficultyMultiplier = draft.DifficultyMultiplier * (1.0 + additionalMultiplier)
                };
            }

            encounterDraftDto = CombatEncounterDraftDto.FromDomain(draft);
            var skillEffects = await BuildSkillEffectsAsync(run, draft, cancellationToken);

            var combatRuntime = _combatFactory.CreateFromDraft(
                combatId, draft, run.PlayerState, run.RunModifiers,
                attackPower: run.Attack, defense: run.Defense, speed: run.Speed,
                palaceRoomState: room.PalaceState, focus: run.Focus,
                skillEffects: skillEffects);

            // ATB: bake Markov tempo + opening gauges, then elect the opener.
            _atbPreparer.PrepareNewCombat(combatRuntime, run);

            run.StartCombat(combatRuntime);
            // ATB: enemy turns (including the opening, if an enemy is up first) are
            // driven by the client in real time via AdvanceCombatTurnCommand.
            if (combatRuntime.Status != CombatStatus.Active)
                pendingRewardOffer = await _combatResolution.ApplyOutcomeAsync(run, combatRuntime, _clock.UtcNow, cancellationToken);

            combatRuntimeDto = CombatRuntimeDto.FromDomain(
                combatRuntime, CombatItemHelper.GetUsableBattleItems(run));
        }
        else if (selectedNode.EventType == NodeEventType.Item)
        {
            var itemRewardOffer = _rewardOfferFactory.CreateItemRewardOffer(
                selectedNode.RewardProfile,
                selectedNode.RiskLevel);

            run.SetPendingRewardOffer(itemRewardOffer.Id);
            pendingRewardOffer = itemRewardOffer;
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("item");
        }
        else if (selectedNode.EventType == NodeEventType.Merchant)
        {
            var merchantRewardOffer = _rewardOfferFactory.CreateMerchantRewardOffer(
                selectedNode.RiskLevel);

            run.SetPendingRewardOffer(merchantRewardOffer.Id);
            pendingRewardOffer = merchantRewardOffer;
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("trade");
        }
        else if (selectedNode.EventType == NodeEventType.Law)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);
            resolutionResult = ApplyPalaceLawContent(resolutionResult, (ResolvedPalaceLawEventContent)resolvedContent);
            run.ResolveCurrentEvent();
        }
        else if (selectedNode.EventType == NodeEventType.Npc)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);
            var npcContent = (ResolvedNpcEventContent)resolvedContent;
            resolutionResult = ApplyNpcContent(resolutionResult, npcContent);

            var npcRelationship = run.BeginOrResumeNpcEncounter(npcContent.NpcProfileKey);
            npcDialogue = await BuildNpcDialogueAsync(npcContent.NpcProfileKey, npcRelationship, cancellationToken);

            if (npcDialogue is not null)
            {
                resolutionResult = resolutionResult with
                {
                    Title = npcDialogue.Speaker,
                    RequiresPlayerChoice = npcDialogue.Choices.Count > 0,
                    Choices = npcDialogue.Choices,
                    NarrativeFragments = npcDialogue.Lines
                        .Select(line => new NarrativeFragmentDto(npcDialogue.Speaker, line))
                        .ToArray()
                };
            }

            run.ResolveCurrentEvent();
        }
        else
        {
            run.ResolveCurrentEvent();
        }

        await _runRepository.UpdateAsync(run, cancellationToken);
        if (pendingRewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, pendingRewardOffer, cancellationToken);
        }

        var outcome = ResolvedNodeEventOutcomeDto.FromResult(
            selectedNode,
            resolutionResult);

        return new ResolveCurrentEventResponse(
            RunDto.FromDomain(run),
            outcome,
            encounterDraftDto,
            combatRuntimeDto,
            npcDialogue);
    }

    private async Task<NpcDialogueViewDto?> BuildNpcDialogueAsync(
    string npcKey,
    Leds.GameEngine.Domain.Npcs.NpcRelationship relationship,
    CancellationToken cancellationToken)
    {
        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, npcKey, StringComparison.OrdinalIgnoreCase));
        return npc is null ? null : Leds.GameEngine.Application.Events.NpcDialogueViewFactory.Build(npc, relationship);
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
            RoomClimate: ResolveActiveClimate(run, room),
            PartyAllies: BuildPartyAllies(run));

        return await _encounterDraftGenerator.GenerateAsync(
            draftContext, cancellationToken);
    }

    // Builds the combat party from the run roster: Characters[0] is the protagonist
    // (its stats already drive PlayerState/run stats), the rest are companions
    // (up to 4), each with its own kit. Total party is capped at 5.
    private static IReadOnlyCollection<CombatEncounterDraftAlly> BuildPartyAllies(Run run)
    {
        var characters = run.PlayerSnapshot?.Characters.ToArray() ?? [];
        if (characters.Length == 0)
        {
            return []; // generator falls back to the default protagonist
        }

        var allies = new List<CombatEncounterDraftAlly>(capacity: 5);

        var protagonist = characters[0];
        allies.Add(new CombatEncounterDraftAlly(
            AllyKey: "player.self",
            DisplayName: protagonist.DisplayName,
            Role: "Protagonist",
            Tags: new[] { "player", "protagonist" },
            IsProtagonist: true,
            MaxVitality: protagonist.StatBlock.MaxVitality,
            AttackPower: protagonist.StatBlock.AttackPower,
            Defense: protagonist.StatBlock.Defense,
            StartingGuard: protagonist.StatBlock.StartingGuard,
            Speed: protagonist.StatBlock.Speed,
            Initiative: protagonist.StatBlock.Initiative,
            Recovery: protagonist.StatBlock.Recovery,
            Focus: protagonist.StatBlock.Focus,
            Mana: protagonist.StatBlock.Mana,
            Charge: protagonist.StatBlock.Charge,
            Skills: MapCharacterSkills(protagonist)));

        foreach (var companion in characters.Skip(1).Take(4))
        {
            allies.Add(new CombatEncounterDraftAlly(
                AllyKey: $"companion.{companion.CharacterId:N}",
                DisplayName: companion.DisplayName,
                Role: "Companion",
                Tags: new[] { "companion" },
                IsProtagonist: false,
                MaxVitality: companion.StatBlock.MaxVitality,
                AttackPower: companion.StatBlock.AttackPower,
                Defense: companion.StatBlock.Defense,
                StartingGuard: companion.StatBlock.StartingGuard,
                Speed: companion.StatBlock.Speed,
                Initiative: companion.StatBlock.Initiative,
                Recovery: companion.StatBlock.Recovery,
                Focus: companion.StatBlock.Focus,
                Mana: companion.StatBlock.Mana,
                Charge: companion.StatBlock.Charge,
                Skills: MapCharacterSkills(companion)));
        }

        return allies;
    }

    private async Task<IReadOnlyDictionary<string, SkillStatusEffectSpec>> BuildSkillEffectsAsync(
    Run run, CombatEncounterDraft draft, CancellationToken ct)
    {
        var keys = draft.Enemies.SelectMany(e => e.SkillKeys)
            .Concat(run.PlayerState?.Skills.Select(s => s.Key) ?? Enumerable.Empty<string>())
            .Concat(run.PlayerSnapshot?.Characters.SelectMany(c => c.Skills.Select(s => s.SkillDefinitionKey))
                    ?? Enumerable.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dict = new Dictionary<string, SkillStatusEffectSpec>(StringComparer.OrdinalIgnoreCase);
        if (keys.Length == 0) return dict;

        var defs = await _catalogContentGateway.ListSkillDefinitionsByKeysAsync(keys, ct);
        foreach (var d in defs)
        {
            var spec = ToStatusSpec(d);
            if (spec is not null) dict[d.Key] = spec;
        }
        return dict;
    }

    private static SkillStatusEffectSpec? ToStatusSpec(CatalogSkillDefinition d)
    {
        if (string.IsNullOrWhiteSpace(d.EffectKind)
            || !Enum.TryParse<StatusEffectKind>(d.EffectKind, true, out var kind))
            return null;

        var stat = CombatStat.None;
        if (!string.IsNullOrWhiteSpace(d.EffectStat))
            Enum.TryParse(d.EffectStat, true, out stat);

        var key = string.IsNullOrWhiteSpace(d.EffectStatusKey) ? d.Key : d.EffectStatusKey;
        return new SkillStatusEffectSpec(
            Key: key, DisplayName: key, Kind: kind,
            Magnitude: d.EffectMagnitude, DurationTicks: d.EffectDurationTicks,
            TickInterval: d.EffectTickInterval, Stat: stat, EmotionalType: null, Stacks: 1);
    }

    private static IReadOnlyCollection<CombatEncounterDraftSkill> MapCharacterSkills(RunCharacterSnapshot character)
    {
        return character.Skills
            .Select(s => new CombatEncounterDraftSkill(
                Key: s.SkillDefinitionKey,
                DisplayName: s.DisplayName,
                Description: string.Empty,
                SkillType: s.SkillType,
                TargetingType: s.TargetingMode,
                EffectType: s.EffectType,
                ManaCost: s.ManaCost,
                ChargeCost: s.ChargeCost,
                BasePower: s.BasePower,
                Tags: Array.Empty<string>()))
            .ToArray();
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
                .ToArray(),
            PalaceRoomState: room.PalaceState,
            RoomClimate: ResolveActiveClimate(run, room));

        var contentResult = await _eventContentResolver.ResolveAsync(
            contentContext, cancellationToken);

        if (contentResult.IsFailure)
        {
            throw new DomainException(
                $"Failed to resolve event content: {contentResult.Error.Message}");
        }

        return contentResult.Value;
    }

    private static NodeEventResolutionResult ApplyNpcContent(
        NodeEventResolutionResult result,
        ResolvedNpcEventContent content)
    {
        if (string.IsNullOrWhiteSpace(content.NpcDisplayName))
            return result;

        return result with
        {
            Title = content.NpcDisplayName,
            Description = content.NpcDescription
        };
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
