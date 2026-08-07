using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

/// <summary>
/// Resolves a player's choice in an NPC encounter against the NPC's authored dialogue
/// graph: applies the chosen choice's consequences (state-conditioned by the NPC's
/// fracture), evaluates transgressions, refreshes wound states, advances the graph,
/// and performs deterministic reward/curse rolls with real effect application.
/// Unique combat remains deferred to a dedicated wave.
/// </summary>
public sealed class NpcEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public NpcEventChoiceResolver(ICatalogContentGateway catalogContentGateway, IPlayerProfileGateway playerProfileGateway)
    {
        _catalogContentGateway = catalogContentGateway;
        _playerProfileGateway = playerProfileGateway;
    }

    public NodeEventType EventType => NodeEventType.Npc;

    public async Task<CurrentEventChoiceResolutionResult> ResolveAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var run = context.Run;
        var npcKey = run.ActiveNpcKey;

        if (string.IsNullOrWhiteSpace(npcKey))
        {
            return Fade(context.ChoiceId);
        }

        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, npcKey, StringComparison.OrdinalIgnoreCase));

        if (npc?.DialogueGraph is null)
        {
            run.EndNpcEncounter();
            return Fade(context.ChoiceId);
        }

        var graph = npc.DialogueGraph;
        var relationship = run.GetNpcRelationship(npcKey) ?? run.BeginOrResumeNpcEncounter(npcKey);

        var currentNodeKey = relationship.CurrentDialogueNodeKey ?? graph.EntryNodeKey;
        if (!graph.Nodes.TryGetValue(currentNodeKey, out var node))
        {
            run.EndNpcEncounter();
            return Fade(context.ChoiceId);
        }

        var choice = node.Choices.FirstOrDefault(c =>
            string.Equals(c.Key, context.ChoiceId, StringComparison.OrdinalIgnoreCase));

        if (choice is null)
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId, accepted: false,
                $"Choice '{context.ChoiceId}' is not available right now.",
                encounterCompleted: false);
        }

        if (!RequirementsMet(choice.Requirements, relationship, run))
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId, accepted: false,
                "This path is closed to you.",
                encounterCompleted: false);
        }

        var fragments = new List<NarrativeFragmentDto>();
        var effects = new List<AppliedConsequenceEffect>();
        var conditioningState = relationship.AggregateState;

        var applicable = choice.Consequences
            .Where(c => c.WhenWoundState is null ||
                        string.Equals(c.WhenWoundState, conditioningState.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        IReadOnlyCollection<CatalogRewardCursePool> pools =
            applicable.Any(c => c.Kind == "RewardOrCurseRoll")
                ? await _catalogContentGateway.ListRewardCursePoolsAsync(cancellationToken)
                : [];

        foreach (var consequence in applicable)
        {
            await ApplyConsequenceAsync(consequence, npc, run, context.Node, relationship, pools, fragments, effects, cancellationToken);
        }

        // Every resolved choice, with any PNJ, grants a stat point — uncapped by design:
        // this is the game's primary, ever-available source of stat point income.
        await _playerProfileGateway.AwardStatPointsAsync(run.PlayerId, 1, cancellationToken);
        fragments.Add(new NarrativeFragmentDto(npc.DisplayName, "Cette rencontre t'apprend quelque chose sur toi-même. +1 point de compétence."));
        effects.Add(new AppliedConsequenceEffect("statPoint", 1, npc.DisplayName));

        EvaluateTransgressions(npc, run, relationship);
        RefreshWounds(npc, run, relationship);

        relationship.AdvanceTo(choice.NextNodeKey);
        var encounterCompleted = string.IsNullOrWhiteSpace(choice.NextNodeKey);
        if (encounterCompleted)
        {
            run.EndNpcEncounter();
        }

        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId, accepted: true,
            encounterCompleted ? "La rencontre se referme." : "La conversation se poursuit.",
            fragments, encounterCompleted, effects);
    }

    private async Task ApplyConsequenceAsync(
       CatalogDialogueConsequence consequence,
       CatalogNpcDefinition npc,
       Run run,
       MapNode node,
       NpcRelationship relationship,
       IReadOnlyCollection<CatalogRewardCursePool> pools,
       List<NarrativeFragmentDto> fragments,
       List<AppliedConsequenceEffect> effects,
       CancellationToken cancellationToken)
    {
        switch (consequence.Kind)
        {
            case "Narrative":
                if (!string.IsNullOrWhiteSpace(consequence.NarrativeFragmentKey))
                {
                    fragments.Add(new NarrativeFragmentDto(npc.DisplayName, consequence.NarrativeFragmentKey));
                }
                break;

            case "AdjustRelationship":
                var appliedDelta = run.ScaleReputationGain(consequence.RelationshipDelta, npc.Key);
                relationship.AdjustScore(appliedDelta);
                if (appliedDelta != 0)
                {
                    effects.Add(new AppliedConsequenceEffect("reputation", appliedDelta, npc.DisplayName));
                }
                break;

            case "SetMemoryFlag":
                if (!string.IsNullOrWhiteSpace(consequence.MemoryFlag))
                {
                    relationship.SetFlag(consequence.MemoryFlag);
                }
                break;

            case "ArmWound":
                if (!string.IsNullOrWhiteSpace(consequence.WoundKey))
                {
                    relationship.SetWoundState(consequence.WoundKey, WoundState.Rompu, canRevert: false);
                }
                break;

            case "SootheWound":
                if (!string.IsNullOrWhiteSpace(consequence.WoundKey))
                {
                    var wound = npc.Wounds?.FirstOrDefault(w =>
                        string.Equals(w.Key, consequence.WoundKey, StringComparison.OrdinalIgnoreCase));
                    var canRevert = wound is not null &&
                        !string.Equals(wound.Reversibility, "Irreversible", StringComparison.OrdinalIgnoreCase) &&
                        !IsWoundHealingBlocked(run);
                    relationship.SetWoundState(consequence.WoundKey, WoundState.Latent, canRevert);
                }
                break;

            case "RewardOrCurseRoll":
                fragments.Add(await RollRewardOrCurseAsync(consequence, npc, run, node, relationship, pools, effects, cancellationToken));
                break;

            case "TriggerUniqueCombat":
                fragments.Add(new NarrativeFragmentDto(npc.DisplayName, "L'air se tend — une confrontation s'ouvre."));
                break;

            case "GrantOffering":
                fragments.Add(await GrantOfferingAsync(consequence, npc, run, relationship, cancellationToken));
                break;

            case "PersistReputationMilestone":
                if (!string.IsNullOrWhiteSpace(consequence.MemoryFlag))
                {
                    relationship.SetFlag(consequence.MemoryFlag);
                    await _playerProfileGateway.GrantReputationMilestoneAsync(
                        run.PlayerId, npc.Key, consequence.MemoryFlag, run.Id.Value, cancellationToken);
                }
                break;

            default:
                break;
        }
    }

    // ── Offres (compétence / objet / point de compétence) ────────────────────

    private async Task<NarrativeFragmentDto> GrantOfferingAsync(
        CatalogDialogueConsequence consequence,
        CatalogNpcDefinition npc,
        Run run,
        NpcRelationship relationship,
        CancellationToken cancellationToken)
    {
        var offering = npc.Offerings?.FirstOrDefault(o =>
            string.Equals(o.Key, consequence.OfferingKey, StringComparison.OrdinalIgnoreCase));

        if (offering is null || !RequirementsMet(offering.UnlockConditions, relationship, run))
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        if (offering.IsMajor)
        {
            var alreadyClaimed = await _playerProfileGateway.HasClaimedNpcOfferingAsync(
                run.PlayerId, npc.Key, offering.Key, cancellationToken);
            if (alreadyClaimed)
            {
                return new NarrativeFragmentDto(npc.DisplayName, "« Je t'ai déjà donné ce que j'avais à donner. »");
            }
        }

        var text = await ApplyOfferingAsync(offering, npc, run, cancellationToken);

        if (offering.IsMajor)
        {
            await _playerProfileGateway.ClaimNpcOfferingAsync(
                run.PlayerId, npc.Key, offering.Key, run.Id.Value, cancellationToken);
        }

        return new NarrativeFragmentDto(npc.DisplayName, text);
    }

    private async Task<string> ApplyOfferingAsync(
        CatalogNpcOffering offering, CatalogNpcDefinition npc, Run run, CancellationToken cancellationToken)
    {
        switch (offering.Kind)
        {
            case "Skill":
                if (string.IsNullOrWhiteSpace(offering.TargetKey) || run.PlayerSnapshot is null)
                {
                    return "Rien ne se produit.";
                }

                var protagonistId = run.PlayerSnapshot.Characters.First().CharacterId;
                await _playerProfileGateway.UnlockSkillAsync(
                    run.PlayerId, protagonistId, offering.TargetKey, cancellationToken, source: $"npc:{npc.Key}");
                return $"Une nouvelle compétence s'inscrit en toi — {offering.TargetKey}.";

            case "StatPoint":
                var amount = offering.Amount > 0 ? offering.Amount : 1;
                await _playerProfileGateway.AwardStatPointsAsync(run.PlayerId, amount, cancellationToken);
                return $"Tu sens ta détermination grandir. +{amount} point de compétence.";

            case "Currency":
                var currencyAmount = offering.Amount > 0 ? offering.Amount : 1;

                // "Loi du Prêteur" (law.preteur): currency gains are boosted while active.
                var currencyGainBonusPercent = run.RunModifiers
                    .Where(m => m.Type == RunModifierType.CurrencyGainBonusPercent && !m.IsConsumed)
                    .Sum(m => m.Value);
                var boostedCurrencyAmount = (int)Math.Round(
                    currencyAmount * (1 + currencyGainBonusPercent / 100.0));

                await _playerProfileGateway.AwardCurrencyAsync(run.PlayerId, boostedCurrencyAmount, cancellationToken);
                return $"+{boostedCurrencyAmount} Éclats du Palais.";

            case "Companion":
                if (string.IsNullOrWhiteSpace(offering.TargetKey))
                {
                    return "Rien ne se produit.";
                }

                var kit = offering.CompanionKit
                    ?? throw new DomainException(
                        $"Companion offering '{offering.Key}' has no Catalog kit.");
                await _playerProfileGateway.RecruitCompanionAsync(
                    run.PlayerId, offering.TargetKey, npc.DisplayName,
                    maxVitality: kit.MaxVitality,
                    attackPower: kit.AttackPower,
                    defense: kit.Defense,
                    startingGuard: kit.StartingGuard,
                    speed: kit.Speed,
                    initiative: kit.Initiative,
                    focus: kit.Focus,
                    mana: kit.Mana,
                    charge: kit.Charge,
                    skillKeys: kit.SkillKeys,
                    cancellationToken,
                    magicAttack: kit.MagicAttack,
                    magicDefense: kit.MagicDefense);
                return $"{npc.DisplayName} se joint à vous, désormais — pour de bon.";

            case "ReputationBoost":
                if (string.IsNullOrWhiteSpace(offering.TargetKey))
                {
                    return "Rien ne se produit.";
                }

                var boost = offering.Amount > 0 ? offering.Amount : 0;
                run.AdjustNpcRelationshipScore(offering.TargetKey, boost);
                return $"Un mot glissé en votre faveur — {offering.TargetKey} vous voit désormais autrement.";

            case "Item":
                if (string.IsNullOrWhiteSpace(offering.TargetKey))
                {
                    return "Rien ne se produit.";
                }

                var itemResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(offering.TargetKey, cancellationToken);
                if (itemResult.IsFailure)
                {
                    return "Rien ne se produit.";
                }

                var itemDef = itemResult.Value;
                // Category/ItemType/Rarity are free-authored strings in the catalog (not enum-backed
                // at rest) — mapping them straight through Enum.Parse throws for almost any real
                // catalog value (e.g. ItemType "Container"/"Potion", Rarity "Legendary"/"Unique" has
                // no RunItemRarity equivalent). Map defensively instead of trusting an exact match.
                run.AddRunItem(RunItem.Create(
                    itemDef.Key, itemDef.DisplayName, itemDef.Description,
                    CatalogRunItemMapper.MapType(itemDef.Category),
                    CatalogRunItemMapper.MapRarity(itemDef.Rarity),
                    quantity: offering.Amount > 0 ? offering.Amount : 1,
                    CatalogRunItemMapper.MapEffect(itemDef.EffectRunType),
                    effectAmount: itemDef.EffectValue,
                    isContainer: itemDef.IsContainer,
                    containerCapacity: itemDef.ContainerCapacity,
                    isLiquid: itemDef.IsLiquid));
                return $"{npc.DisplayName} te tend {itemDef.DisplayName}.";

            default:
                return "Rien ne se produit.";
        }
    }

    // ── Reward / curse roll (deterministic) + real application ───────────────

    private async Task<NarrativeFragmentDto> RollRewardOrCurseAsync(
    CatalogDialogueConsequence consequence,
    CatalogNpcDefinition npc,
    Run run,
    MapNode node,
    NpcRelationship relationship,
    IReadOnlyCollection<CatalogRewardCursePool> pools,
    List<AppliedConsequenceEffect> effects,
    CancellationToken cancellationToken)
    {
        var poolKey = consequence.RewardCursePoolKey;
        var pool = poolKey is null
            ? null
            : pools.FirstOrDefault(p => string.Equals(p.Key, poolKey, StringComparison.OrdinalIgnoreCase));

        if (pool is null || pool.Entries.Count == 0)
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        var eligible = pool.Entries.Where(e => IsAvailable(e, run, node)).ToArray();
        if (eligible.Length == 0)
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        var seed = string.Join('|',
            run.Seed, "npc-reward-curse", relationship.NpcKey, poolKey,
            consequence.WhenWoundState ?? "any", relationship.TimesMet.ToString());
        var roll = DeterministicCombatRoll.UnitInterval(seed);
        var index = Math.Min(eligible.Length - 1, (int)(roll * eligible.Length));
        var entry = eligible[index];

        var effect = await ApplyRewardCurseEffectAsync(entry, run, cancellationToken);
        if (effect is null)
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        effects.Add(effect);

        var text = effect.Kind switch
        {
            "heal" => $"Une chaleur t'apaise. +{effect.Amount} vitalité.",
            "damage" => $"Le poison te ronge. −{effect.Amount} vitalité.",
            "curse" => $"Une ombre s'installe — {effect.Label}.",
            "law" => $"Une Loi s'inscrit dans le Palais — {effect.Label}.",
            _ => "Quelque chose change en toi."
        };
        return new NarrativeFragmentDto(npc.DisplayName, text);
    }

    private async Task<AppliedConsequenceEffect?> ApplyRewardCurseEffectAsync(
    CatalogRewardCurseEntry entry,
    Run run,
    CancellationToken cancellationToken)
    {
        switch (entry.ResultKind)
        {
            case "Heal":
                if (entry.Amount <= 0) return null;
                run.ApplyHeal(entry.Amount);
                return new AppliedConsequenceEffect("heal", entry.Amount, "Vitalité");

            case "Damage":
                if (entry.Amount <= 0) return null;
                NpcConsequenceApplier.ApplyDamage(run, entry.Amount);
                return new AppliedConsequenceEffect("damage", entry.Amount, "Poison");

            case "GrantCurse":
                if (string.IsNullOrWhiteSpace(entry.TargetKey)) return null;
                var curseResult = await _catalogContentGateway.GetCurseDefinitionByKeyAsync(entry.TargetKey, cancellationToken);
                if (curseResult.IsFailure) return null;
                NpcConsequenceApplier.ApplyCurse(run, curseResult.Value);
                return new AppliedConsequenceEffect("curse", 0, curseResult.Value.DisplayName);

            case "GrantLaw":
                if (string.IsNullOrWhiteSpace(entry.TargetKey)) return null;
                var lawResult = await _catalogContentGateway.GetPalaceLawDefinitionByKeyAsync(entry.TargetKey, cancellationToken);
                if (lawResult.IsFailure) return null;
                NpcConsequenceApplier.ApplyLaw(run, lawResult.Value);
                return new AppliedConsequenceEffect("law", 0, lawResult.Value.Name);

            default:
                return null;
        }
    }

    private static bool IsAvailable(CatalogRewardCurseEntry entry, Run run, MapNode node)
    {
        if (entry.Availability is null || entry.Availability.Count == 0)
        {
            return true;
        }

        var vitalityRatio = run.MaxHp > 0 ? (int)(100L * run.CurrentHp / run.MaxHp) : 0;

        foreach (var gate in entry.Availability)
        {
            var ok = gate.Kind switch
            {
                "MinVitalityRatioPercent" => vitalityRatio >= gate.Value,
                "MaxVitalityRatioPercent" => vitalityRatio <= gate.Value,
                "MinActiveLawCount" => run.ActivePalaceLaws.Count >= gate.Value,
                "MinNodeDepth" => node.Row >= gate.Value,
                _ => true
            };

            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    // ── Fracture / requirements ──────────────────────────────────────────────

    private static bool RequirementsMet(
        IReadOnlyCollection<CatalogDialogueRequirement> requirements,
        NpcRelationship relationship,
        Run run)
    {
        foreach (var requirement in requirements)
        {
            switch (requirement.Kind)
            {
                case "FlagPresent":
                    if (requirement.FlagKey is null || !relationship.HasFlag(requirement.FlagKey)) return false;
                    break;
                case "FlagAbsent":
                    if (requirement.FlagKey is not null && relationship.HasFlag(requirement.FlagKey)) return false;
                    break;
                case "WoundStateAtLeast":
                    if (requirement.WoundKey is null ||
                        !Enum.TryParse<WoundState>(requirement.RequiredWoundState, ignoreCase: true, out var required) ||
                        relationship.GetWoundState(requirement.WoundKey) < required)
                    {
                        return false;
                    }
                    break;
                case "RelationshipScoreAtLeast":
                    if (requirement.RequiredRelationshipScore is not int minScore ||
                        relationship.RelationshipScore < minScore)
                    {
                        return false;
                    }
                    break;
                case "PlayerHasContainerItem":
                    if (!run.RunItems.Any(item => item.IsContainer))
                    {
                        return false;
                    }
                    break;
                case "PlayerStatsBalanced":
                    if (!IsPlayerStatsBalanced(run)) return false;
                    break;
                case "PlayerStatsUnbalanced":
                    if (IsPlayerStatsBalanced(run)) return false;
                    break;
                case "PlayerHasCompanion":
                    if (requirement.FlagKey is null || !HasCompanion(run, requirement.FlagKey)) return false;
                    break;
                case "PlayerLacksCompanion":
                    if (requirement.FlagKey is not null && HasCompanion(run, requirement.FlagKey)) return false;
                    break;
            }
        }

        return true;
    }

    // "Besoin d'optimisation" (l'Architecte) : au-delà de ~50% d'écart entre la stat
    // la plus forte et la plus faible (Attaque/Défense/Vitesse/Focus), la progression
    // du joueur est jugée trop inégale. Seuil authored, ajustable.
    private static bool IsPlayerStatsBalanced(Run run)
    {
        var stats = new[] { run.Attack, run.Defense, run.Speed, run.Focus };
        var max = stats.Max();
        var min = stats.Min();
        return max <= 0 || (max - min) / (double)max <= 0.5;
    }

    private static bool HasCompanion(Run run, string companionDefinitionKey)
        => run.PlayerSnapshot?.Characters.Any(c =>
            string.Equals(c.DefinitionKey, companionDefinitionKey, StringComparison.OrdinalIgnoreCase)) == true;

    private static void EvaluateTransgressions(CatalogNpcDefinition npc, Run run, NpcRelationship relationship)
    {
        if (npc.Wounds is null) return;

        foreach (var wound in npc.Wounds)
        {
            foreach (var transgression in wound.Transgressions)
            {
                var marker = $"__armed:{wound.Key}:{transgression.TriggerFlag}";
                if (relationship.HasFlag(transgression.TriggerFlag) && !relationship.HasFlag(marker))
                {
                    // "Loi du Nom Retenu" (RunModifierType.ReputationChangeDoubled) doubles
                    // transgression penalties too, not just gains — routed through the same
                    // ScaleReputationGain used for positive dialogue consequences.
                    relationship.AdjustScore(run.ScaleReputationGain(transgression.RelationshipPenalty, npc.Key));
                    relationship.SetWoundState(wound.Key, WoundState.Rompu, canRevert: false);
                    relationship.SetFlag(marker);
                }
            }
        }
    }

    private static void RefreshWounds(CatalogNpcDefinition npc, Run run, NpcRelationship relationship)
    {
        if (npc.Wounds is null) return;

        var woundHealingBlocked = IsWoundHealingBlocked(run);
        foreach (var wound in npc.Wounds)
        {
            var canRevert = string.Equals(wound.Reversibility, "SoothableByScore", StringComparison.OrdinalIgnoreCase)
                && !woundHealingBlocked;
            relationship.RefreshFromScore(wound.Key, wound.TenseThreshold, wound.RuptureThreshold, canRevert);
        }
    }

    /// <summary>"Loi du Témoin" (RunModifierType.WoundHealingBlocked) — armed wounds cannot
    /// be soothed for the floor, either by act or by score; worsening is unaffected since
    /// NpcRelationship.SetWoundState always allows a state that's >= the current one
    /// regardless of canRevert.</summary>
    private static bool IsWoundHealingBlocked(Run run)
        => run.GetActiveModifiers(RunModifierType.WoundHealingBlocked).Count > 0;

    private static CurrentEventChoiceResolutionResult Fade(string choiceId)
    {
        return CurrentEventChoiceResolutionResult.Create(
            choiceId, accepted: true,
            "La figure s'efface.",
            new[] { new NarrativeFragmentDto("Elise", "Certaines présences ne font que passer.") },
            encounterCompleted: true);
    }
}
