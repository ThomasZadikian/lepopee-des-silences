using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Tactical;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Events.Resolvers;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
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
    private readonly ICombatEncounterDraftGenerator _encounterDraftGenerator;
    private readonly ICombatFactory _combatFactory;
    private readonly ITacticalCombatFactory _tacticalCombatFactory;
    private readonly ITacticalEnemyTurnDriver _tacticalEnemyTurns;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory _rewardOfferFactory;
    private readonly IClock _clock;

    public ResolveCurrentEventCommandHandler(
        IRunRepository runRepository,
        INodeEventResolverDispatcher nodeEventResolverDispatcher,
        IEventContentResolver eventContentResolver,
        ICatalogContentGateway catalogContentGateway,
        ICombatEncounterDraftGenerator encounterDraftGenerator,
        ICombatFactory combatFactory,
        ITacticalCombatFactory tacticalCombatFactory,
        ITacticalEnemyTurnDriver tacticalEnemyTurns,
        IRewardOfferRepository rewardOfferRepository,
        Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory rewardOfferFactory,
        IClock clock)
    {
        _runRepository = runRepository;
        _nodeEventResolverDispatcher = nodeEventResolverDispatcher;
        _eventContentResolver = eventContentResolver;
        _catalogContentGateway = catalogContentGateway;
        _encounterDraftGenerator = encounterDraftGenerator;
        _combatFactory = combatFactory;
        _tacticalCombatFactory = tacticalCombatFactory;
        _tacticalEnemyTurns = tacticalEnemyTurns;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
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
        var selectedNode = room.CurrentSelectedNode;

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
            or NodeEventResolutionKind.RareCombatStarted
            or NodeEventResolutionKind.FinalBossEncounterStarted;

        CombatEncounterDraftDto? encounterDraftDto = null;
        TacticalCombatRuntimeDto? tacticalCombatDto = null;
        IReadOnlyList<TacticalCombatEventDto>? tacticalEvents = null;
        ResolvedNodeEventContent? resolvedContent = null;
        RewardOffer? pendingRewardOffer = null;
        NpcDialogueViewDto? npcDialogue = null;

        if (isCombat)
        {
            var isFinalBoss = resolutionResult.ResolutionKind == NodeEventResolutionKind.FinalBossEncounterStarted;

            if (!isFinalBoss)
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
            }
            else
            {
                // NodeEventType.FinalBoss is intentionally excluded from the standard content
                // pipeline (its content is authored directly by FinalBossNodeEventResolver, not
                // reward-profile-driven like the other combat tiers). Him'Lit speaks here: his
                // attitude depends on how many times he's already been met and on the state of
                // the room preceding his (his own room is always Neutral by construction).
                var himlitRelationship = run.BeginOrResumeNpcEncounter("npc.himlit");
                var himlitFragments = await BuildHimLitTauntFragmentsAsync(
                    himlitRelationship, run, room, cancellationToken);

                if (himlitFragments.Count > 0)
                {
                    resolutionResult = resolutionResult with
                    {
                        NarrativeFragments = resolutionResult.NarrativeFragments
                            .Concat(himlitFragments)
                            .ToArray()
                    };
                }

                // Unlike NodeEventType.Npc (see NpcEventChoiceResolver, which always ends the
                // encounter at every exit point), this FinalBoss branch never asked for player
                // choices, so there's no later choice-resolution step to close it out — end it
                // here instead, otherwise Run.ActiveNpcKey stays dangling on "npc.himlit" until
                // the next real NPC encounter overwrites it.
                run.EndNpcEncounter();
            }

            var combatId = CombatId.New();

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
            var conditionalEquipmentEffects = await ResolveConditionalEquipmentEffectsAsync(run, cancellationToken);

            var roster = _combatFactory.BuildRoster(
                combatId, draft, run.PlayerState, run.RunModifiers,
                attackPower: run.Attack, defense: run.Defense, speed: run.Speed,
                palaceRoomState: room.PalaceState, focus: run.Focus,
                magicAttack: run.MagicAttack, magicDefense: run.MagicDefense,
                skillEffects: skillEffects,
                typedDamageReductions: ToTypedDamageReductions(run.TypedDamageReductions),
                hitChanceBonusPercent: run.HitChanceBonusPercent,
                dotDurationReductionPercent: run.DotDurationReductionPercent,
                dotDamageReductionPercent: run.DotDamageReductionPercent,
                dotDamageBonusPercent: run.DotDamageBonusPercent,
                magicDamageBonusPercent: run.MagicDamageBonusPercent,
                magicDamageReductionPercent: run.MagicDamageReductionPercent,
                criticalChanceBonusPercent: run.CriticalChanceBonusPercent,
                guardBonusPercent: run.GuardBonusPercent,
                himLitProtectionEnabled: run.HimLitProtectionEnabled,
                healingBonusPercent: run.HealingBonusPercent,
                forgottenSkillKey: run.ForgottenSkillKey,
                roomTheme: room.Theme,
                conditionalEquipmentEffects: conditionalEquipmentEffects);

            var tacticalCombat = _tacticalCombatFactory.CreateFromRoster(
                combatId, roster, room, selectedNode.Id, run, _clock.UtcNow.UtcDateTime);

            run.StartTacticalCombat(tacticalCombat);

            // Si la créature la plus rapide ouvre le bal, elle doit jouer maintenant :
            // le joueur n'a pas la main, et rien d'autre ne la lui rendrait — le combat
            // resterait figé avant même son premier tour.
            var openingTurns = _tacticalEnemyTurns.PlayWhileEnemyHasInitiative(tacticalCombat);
            tacticalEvents = openingTurns.Events;

            tacticalCombatDto = TacticalCombatRuntimeDto.FromDomain(
                tacticalCombat, CombatItemHelper.GetUsableBattleItems(run));
        }
        else if (selectedNode.EventType == NodeEventType.Item)
        {
            var itemRewardOffer = await _rewardOfferFactory.CreateItemRewardOfferAsync(
                selectedNode.RewardProfile,
                selectedNode.RiskLevel,
                run.RunModifiers,
                run.Seed,
                run.Id.Value,
                selectedNode.Id.Value,
                rerollNonce: 0,
                cancellationToken);

            run.SetPendingRewardOffer(itemRewardOffer.Id);
            pendingRewardOffer = itemRewardOffer;
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("item");
        }
        else if (selectedNode.EventType == NodeEventType.Merchant)
        {
            var merchantRewardOffer = await _rewardOfferFactory.CreateMerchantRewardOfferAsync(
                selectedNode.RiskLevel,
                run.Seed,
                run.Id.Value,
                selectedNode.Id.Value,
                cancellationToken);

            run.SetPendingRewardOffer(merchantRewardOffer.Id);
            pendingRewardOffer = merchantRewardOffer;
            run.ResolveCurrentEvent();
            selectedNode.ChooseEventOption("trade");
        }
        else if (selectedNode.EventType == NodeEventType.Npc)
        {
            resolvedContent = await ResolveEventContentAsync(run, room, selectedNode, cancellationToken);
            var npcContent = (ResolvedNpcEventContent)resolvedContent;
            resolutionResult = ApplyNpcContent(resolutionResult, npcContent);

            var npcRelationship = run.BeginOrResumeNpcEncounter(npcContent.NpcProfileKey);
            npcDialogue = await BuildNpcDialogueAsync(npcContent.NpcProfileKey, npcRelationship, run, cancellationToken)
                ?? new NpcDialogueViewDto(
                    npcContent.NpcProfileKey,
                    npcContent.NpcDisplayName,
                    string.Empty,
                    new[] { "La figure se tient là, silencieuse." },
                    Array.Empty<NodeEventChoiceDto>(),
                    npcRelationship.AggregateState.ToString(),
                    EncounterActive: false);

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
            npcDialogue,
            tacticalCombatDto,
            tacticalEvents);
    }

    private async Task<NpcDialogueViewDto?> BuildNpcDialogueAsync(
    string npcKey,
    Leds.GameEngine.Domain.Npcs.NpcRelationship relationship,
    Run run,
    CancellationToken cancellationToken)
    {
        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, npcKey, StringComparison.OrdinalIgnoreCase));
        return npc is null ? null : Leds.GameEngine.Application.Events.NpcDialogueViewFactory.Build(npc, relationship, run);
    }

    private async Task<IReadOnlyCollection<NarrativeFragmentDto>> BuildHimLitTauntFragmentsAsync(
        Leds.GameEngine.Domain.Npcs.NpcRelationship relationship,
        Run run,
        Room room,
        CancellationToken cancellationToken)
    {
        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, "npc.himlit", StringComparison.OrdinalIgnoreCase));

        if (npc?.DialogueGraph is null)
        {
            return Array.Empty<NarrativeFragmentDto>();
        }

        var precedingRoomState = run.Rooms
            .Where(r => r.Depth == room.Depth - 1)
            .Select(r => r.PalaceState)
            .FirstOrDefault();

        var nodeKey = HimLitDialogueAttitudeResolver.ResolveNodeKey(relationship.TimesMet, precedingRoomState);

        return !npc.DialogueGraph.Nodes.TryGetValue(nodeKey, out var node)
            ? Array.Empty<NarrativeFragmentDto>()
            : node.Lines.Select(line => new NarrativeFragmentDto(node.Speaker, line)).ToArray();
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
            NodeEventResolutionKind.FinalBossEncounterStarted => "FinalBoss",
            _ => "Combat"
        };

        // CombatRiskTier is generated directly on combat-flavored nodes (MapRoomGenerator) —
        // no more rescale from the raw 0-100 RiskLevel needed at resolve time.
        var generatedRiskTier = selectedNode.CombatRiskTier
            ?? Leds.GameEngine.Domain.Combats.RiskTier.Tendu;

        var enemyCount = encounterType switch
        {
            "Elite" => 1,
            "Rare" => 1,
            "RoomBoss" => 1,
            "FinalBoss" => 1,
            _ => generatedRiskTier switch
            {
                Leds.GameEngine.Domain.Combats.RiskTier.Calme => 2,
                Leds.GameEngine.Domain.Combats.RiskTier.Tendu => 3,
                Leds.GameEngine.Domain.Combats.RiskTier.Dangereux
                    or Leds.GameEngine.Domain.Combats.RiskTier.Perilleux => 4,
                _ => 5
            }
        };
        // Standard "Combat" nodes: use the player's chosen/generated tier DIRECTLY —
        // it's already the exact 1-5 RiskTier the catalog/DifficultyMultiplier expect
        // (see the comment above generatedRiskTier). Previously this re-derived the
        // level from enemyCount, whose Dangereux/Perilleux buckets both map to 4
        // enemies — collapsing the two tiers together and silently downgrading a
        // player's "Périlleux" pick to Dangereux's (lower) stat/loot/reputation/eclats
        // multipliers.
        var catalogRiskLevel = encounterType switch
        {
            "Elite" => (int)Leds.GameEngine.Domain.Combats.RiskTier.Dangereux,
            "RoomBoss" or "FinalBoss" => (int)Leds.GameEngine.Domain.Combats.RiskTier.Fatal,
            "Rare" => Math.Max(
                (int)Leds.GameEngine.Domain.Combats.RiskTier.Tendu,
                (int)generatedRiskTier),
            _ => (int)generatedRiskTier
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
            PartyAllies: BuildPartyAllies(run),
            RoomKey: room.CatalogBinding?.Key);

        return await _encounterDraftGenerator.GenerateAsync(
            draftContext, cancellationToken);
    }

    // Builds the combat party from the run roster: Characters[0] is the protagonist
    // (its stats already drive PlayerState/run stats), the rest are companions
    // (up to 3), each with its own kit. Total party is capped at 4.
    private static IReadOnlyCollection<CombatEncounterDraftAlly> BuildPartyAllies(Run run)
    {
        var characters = run.PlayerSnapshot?.Characters.ToArray() ?? [];
        if (characters.Length == 0)
        {
            return []; // generator falls back to the default protagonist
        }

        var allies = new List<CombatEncounterDraftAlly>(capacity: Run.MaxPartySize);

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
            Focus: protagonist.StatBlock.Focus,
            Mana: protagonist.StatBlock.Mana,
            Charge: protagonist.StatBlock.Charge,
            MagicAttack: protagonist.StatBlock.MagicAttack,
            MagicDefense: protagonist.StatBlock.MagicDefense,
            Movement: protagonist.StatBlock.Movement,
            Skills: MapCharacterSkills(protagonist)));

        foreach (var companion in characters.Skip(1).Take(Run.MaxPartySize - 1))
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
                Focus: companion.StatBlock.Focus,
                Mana: companion.StatBlock.Mana,
                Charge: companion.StatBlock.Charge,
                MagicAttack: companion.StatBlock.MagicAttack,
                MagicDefense: companion.StatBlock.MagicDefense,
                Movement: companion.StatBlock.Movement,
                Skills: MapCharacterSkills(companion)));
        }

        return allies;
    }

    /// <summary>
    /// NPC keys of already-recruited companions, derived from the roster's character
    /// definition keys by naming convention (<c>"character.thomas"</c> → <c>"npc.thomas"</c>).
    /// Used to keep a recruited NPC from ever being re-selected for an encounter — see
    /// NpcEncounterSelector.
    /// </summary>
    private static IReadOnlyCollection<string> RecruitedCompanionNpcKeys(Run run)
    {
        const string CharacterKeyPrefix = "character.";
        const string NpcKeyPrefix = "npc.";

        var characters = run.PlayerSnapshot?.Characters.ToArray() ?? [];

        return characters
            .Skip(1) // index 0 is always the protagonist, never an NPC-derived companion
            .Select(c => c.DefinitionKey)
            .Where(key => key.StartsWith(CharacterKeyPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(key => NpcKeyPrefix + key[CharacterKeyPrefix.Length..])
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>> BuildSkillEffectsAsync(
    Run run, CombatEncounterDraft draft, CancellationToken ct)
    {
        var keys = draft.Enemies.SelectMany(e => e.SkillKeys)
            .Concat(run.PlayerState?.Skills.Select(s => s.Key) ?? Enumerable.Empty<string>())
            .Concat(run.PlayerSnapshot?.Characters.SelectMany(c => c.Skills.Select(s => s.SkillDefinitionKey))
                    ?? Enumerable.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dict = new Dictionary<string, IReadOnlyList<SkillStatusEffectSpec>>(StringComparer.OrdinalIgnoreCase);
        if (keys.Length == 0) return dict;

        var defs = await _catalogContentGateway.ListSkillDefinitionsByKeysAsync(keys, ct);
        foreach (var d in defs)
        {
            var specs = ToStatusSpecs(d);
            if (specs.Count > 0) dict[d.Key] = specs;
        }
        return dict;
    }

    /// <summary>
    /// Room/weather-conditional equipment effects (e.g. Boussole du Pèlerin, Couronne de sel)
    /// carried by whatever the protagonist has equipped right now — re-evaluated fresh every
    /// combat by CombatFactory instead of being baked into PlayerStatMerger's static run-start
    /// stats (see that class's Condition filter).
    /// </summary>
    private async Task<IReadOnlyCollection<CatalogItemEquipmentEffect>> ResolveConditionalEquipmentEffectsAsync(
        Run run, CancellationToken ct)
    {
        var equippedItemKeys = run.PlayerSnapshot?.Characters.FirstOrDefault()?.EquippedItemKeys;
        if (equippedItemKeys is not { Count: > 0 })
            return [];

        var itemDefinitions = await _catalogContentGateway.ListActiveItemDefinitionsAsync(ct);
        return itemDefinitions
            .Where(item => equippedItemKeys.Contains(item.Key))
            .SelectMany(item => item.EquipmentEffects ?? [])
            .Where(effect => effect.Condition is not null)
            .ToArray();
    }

    private static IReadOnlyList<SkillStatusEffectSpec> ToStatusSpecs(CatalogSkillDefinition d)
    {
        if (d.Effects is not { Count: > 0 })
            return [];

        var specs = new List<SkillStatusEffectSpec>();
        foreach (var effect in d.Effects)
        {
            if (string.IsNullOrWhiteSpace(effect.Kind)
                || !Enum.TryParse<StatusEffectKind>(effect.Kind, true, out var kind))
                continue;

            var stat = CombatStat.None;
            if (!string.IsNullOrWhiteSpace(effect.Stat))
                Enum.TryParse(effect.Stat, true, out stat);

            // Disambiguate multiple effects on the same skill (e.g. HealOverTime + GuardOverTime
            // both from "Construction perpétuelle") — a shared key would make the second
            // Reinforce() the first as if it were the same status instead of a distinct one.
            // DisplayName stays the skill's own name (not the internal Key, which is a raw
            // catalog key like "canon.skill.regard-infantile:StatModifier" and would otherwise
            // leak straight into player-facing combat log lines).
            var key = string.IsNullOrWhiteSpace(effect.StatusKey) ? $"{d.Key}:{effect.Kind}" : effect.StatusKey;
            specs.Add(new SkillStatusEffectSpec(
                Key: key, DisplayName: d.DisplayName, Kind: kind,
                Magnitude: effect.Magnitude, DurationTicks: effect.DurationTicks,
                TickInterval: effect.TickInterval, Stat: stat, EmotionalType: null, Stacks: 1,
                MagnitudeIsPercentOfMax: effect.MagnitudeIsPercentOfMax,
                MagnitudeIsPercentOfBaseStat: effect.MagnitudeIsPercentOfBaseStat,
                AppliesToActor: effect.AppliesToActor,
                IsPermanent: effect.IsPermanent));
        }
        return specs;
    }

    private static IReadOnlyDictionary<EmotionalType, int> ToTypedDamageReductions(
        IReadOnlyDictionary<string, int> reductions)
    {
        var result = new Dictionary<EmotionalType, int>();
        foreach (var (typeName, percent) in reductions)
        {
            if (Enum.TryParse<EmotionalType>(typeName, true, out var type))
                result[type] = percent;
        }
        return result;
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
            RoomClimate: ResolveActiveClimate(run, room),
            RoomKey: room.CatalogBinding?.Key,
            RecruitedCompanionNpcKeys: RecruitedCompanionNpcKeys(run));

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
