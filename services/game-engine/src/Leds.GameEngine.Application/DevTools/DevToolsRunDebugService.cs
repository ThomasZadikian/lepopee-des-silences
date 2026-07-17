using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.DevTools;

public sealed class DevToolsRunDebugService : IDevToolsRunDebugService
{
    private const int MaxAdvanceRoomCount = 10;
    private const int MaxPartySize = 5;
    private const int MaxDebugVitality = 999;
    private const int MaxDebugGuard = 999;

    private readonly IRunRepository _runRepository;
    private readonly IRunGenerator _runGenerator;
    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;

    public DevToolsRunDebugService(
        IRunRepository runRepository,
        IRunGenerator runGenerator,
        ICatalogContentGateway catalogContentGateway,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository)
    {
        _runRepository = runRepository;
        _runGenerator = runGenerator;
        _catalogContentGateway = catalogContentGateway;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
    }

    public async Task<DevToolsRunDebugResult> AdvanceRoomAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        return await AdvanceRoomsAsync(runId, 1, cancellationToken);
    }


    public async Task<DevToolsRunDebugResult> AdvanceRoomsAsync(
        Guid runId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > MaxAdvanceRoomCount)
            throw new DomainException($"Room advance count must be between 1 and {MaxAdvanceRoomCount}.");

        var run = await GetRunAsync(runId, cancellationToken);
        var advanced = 0;

        for (var i = 0; i < count; i++)
        {
            if (IsClosed(run) || run.CurrentDepth >= 10)
                break;

            run.DebugPrepareForNextRoom();
            var nextRoom = await _runGenerator.GenerateNextRoomAsync(run, cancellationToken);
            run.MoveToNextRoom(nextRoom);
            advanced++;
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new DevToolsRunDebugResult(
            advanced == 1 ? "Advanced 1 room." : $"Advanced {advanced} rooms.",
            RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ForceCurrentRoomPalaceStateAsync(
        Guid runId,
        string state,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PalaceRoomState>(state, ignoreCase: true, out var parsedState))
            throw new DomainException($"Unsupported palace room state '{state}'.");

        var run = await GetRunAsync(runId, cancellationToken);
        run.CurrentRoom.DebugSetPalaceState(parsedState);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new DevToolsRunDebugResult("Current room palace state forced.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ForceCurrentRoomClimateAsync(
        Guid runId,
        string climate,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        var climateValue = MapClimate(climate);

        ClearCurrentRoomClimate(run);

        if (climateValue is not null)
        {
            run.AddRunModifier(RunModifier.Create(
                RunModifierType.RoomClimate,
                climateValue.Value,
                RunModifierDuration.UntilRoomEnds,
                sourceType: "DevTools",
                sourceKey: $"devtools.room-climate.{run.CurrentRoomId.Value}",
                expiresAtRoomId: run.CurrentRoomId.Value));
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new DevToolsRunDebugResult("Current room climate forced.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ActivatePalaceLawAsync(
        Guid runId,
        string lawKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lawKey))
            throw new DomainException("Law key is required.");

        var run = await GetRunAsync(runId, cancellationToken);
        var definitionResult = await _catalogContentGateway.GetPalaceLawDefinitionByKeyAsync(
            lawKey.Trim(), cancellationToken);

        if (definitionResult.IsFailure)
            throw new NotFoundException($"Palace law definition '{lawKey}' was not found.");

        run.ActivatePalaceLaw(PalaceLawMapper.CreatePalaceLaw(definitionResult.Value, PalaceLawDomain.Combat));

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new DevToolsRunDebugResult("Palace law activated.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ClearPalaceLawsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        run.DebugClearActivePalaceLaws();

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsRunDebugResult("Active palace laws cleared.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ActivateCurseAsync(
        Guid runId,
        string curseKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(curseKey))
            throw new DomainException("Curse key is required.");

        var run = await GetRunAsync(runId, cancellationToken);
        var definitionResult = await _catalogContentGateway.GetCurseDefinitionByKeyAsync(
            curseKey.Trim(), cancellationToken);

        if (definitionResult.IsFailure)
            throw new NotFoundException($"Curse definition '{curseKey}' was not found.");

        var definition = definitionResult.Value;

        var difficultyDelta = Math.Clamp(definition.Severity / 100.0, 0.01, 0.5);
        var curse = ActiveCurse.Create(
            definition.Key,
            definition.DisplayName,
            definition.Description,
            difficultyDelta,
            DateTime.UtcNow,
            curseDefinitionKey: definition.Key,
            severity: definition.Severity,
            duration: definition.Duration,
            effectSetKey: definition.EffectSetKey);

        run.ApplyCurse(curse);
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.NextCombatDifficultyMultiplier,
            difficultyDelta,
            RunModifierDuration.NextCombatOnly,
            sourceType: "Curse",
            sourceKey: definition.Key));

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsRunDebugResult("Curse activated.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> ClearCursesAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        run.DebugClearActiveCurse();

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsRunDebugResult("Active curses cleared.", RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> AddDebugAllyAsync(
        Guid runId,
        string companionNpcKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companionNpcKey))
            throw new DomainException("Companion NPC key is required.");

        var run = await GetRunAsync(runId, cancellationToken);
        var snapshot = run.PlayerSnapshot
            ?? throw new DomainException("Run has no player snapshot to add a companion to.");

        if (snapshot.Characters.Count >= MaxPartySize)
            throw new DomainException($"Party is already at the maximum of {MaxPartySize} characters.");

        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, companionNpcKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("NPC definition", companionNpcKey);

        var offering = (npc.Offerings ?? [])
            .FirstOrDefault(o => string.Equals(o.Kind, "Companion", StringComparison.OrdinalIgnoreCase) && o.CompanionKit is not null)
            ?? throw new DomainException($"NPC '{companionNpcKey}' has no recruitable companion kit.");

        var kit = offering.CompanionKit!;

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: kit.MaxVitality,
            attackPower: kit.AttackPower,
            defense: kit.Defense,
            startingGuard: kit.StartingGuard,
            speed: kit.Speed,
            initiative: kit.Initiative,
            recovery: kit.Recovery,
            focus: kit.Focus,
            mana: kit.Mana,
            charge: kit.Charge);

        var skillSnapshots = new List<RunCharacterSkillSnapshot>();
        foreach (var skillKey in kit.SkillKeys)
        {
            var skill = await _catalogContentGateway.GetSkillDefinitionByKeyAsync(skillKey, cancellationToken);
            if (skill is null)
                continue;

            skillSnapshots.Add(RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: skill.Key,
                displayName: skill.DisplayName,
                skillType: skill.SkillType,
                targetingMode: skill.TargetingType,
                effectType: skill.EffectType,
                manaCost: skill.ManaCost,
                chargeCost: skill.ChargeCost,
                basePower: skill.BasePower));
        }

        if (skillSnapshots.Count == 0)
        {
            skillSnapshots.Add(RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingMode: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: 10));
        }

        var character = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(),
            definitionKey: offering.TargetKey ?? npc.Key,
            displayName: npc.DisplayName,
            statBlock: statBlock,
            skills: skillSnapshots);

        snapshot.DebugAddCharacter(character);

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsRunDebugResult(
            $"'{npc.DisplayName}' added to the roster (effective next combat).",
            RunDto.FromDomain(run));
    }

    public async Task<DevToolsRunDebugResult> RemoveDebugAllyAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        var snapshot = run.PlayerSnapshot
            ?? throw new DomainException("Run has no player snapshot.");

        if (!snapshot.DebugRemoveLastCompanion())
            throw new DomainException("No companion to remove (the protagonist cannot be removed).");

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsRunDebugResult(
            "Last companion removed from the roster (effective next combat).",
            RunDto.FromDomain(run));
    }

    public async Task<DevToolsCombatDebugResult> KillAllCurrentCombatEnemiesAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        var combat = GetActiveCombat(run);

        foreach (var enemy in combat.Enemies.Where(enemy => !enemy.IsDefeated))
        {
            enemy.MarkDefeated();
        }

        combat.CompleteIfAllEnemiesDefeated();
        var rewardOffer = await _combatResolution.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);
        if (rewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        return new DevToolsCombatDebugResult("Enemies killed.", CombatRuntimeDto.FromDomain(combat));
    }

    public async Task<DevToolsCombatDebugResult> KillCurrentCombatEnemyAsync(
        Guid runId,
        Guid combatantId,
        CancellationToken cancellationToken = default)
    {
        var run = await GetRunAsync(runId, cancellationToken);
        var combat = GetActiveCombat(run);
        var enemy = combat.Enemies.SingleOrDefault(enemy => enemy.Id.Value == combatantId)
            ?? throw new NotFoundException("Enemy combatant", combatantId);

        if (!enemy.IsDefeated)
            enemy.MarkDefeated();

        combat.CompleteIfAllEnemiesDefeated();
        var rewardOffer = await _combatResolution.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);
        if (rewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        return new DevToolsCombatDebugResult("Enemies killed.", CombatRuntimeDto.FromDomain(combat));
    }

    public async Task<DevToolsCombatDebugResult> SetCurrentCombatantVitalsAsync(
        Guid runId,
        Guid combatantId,
        int vitality,
        int guard,
        CancellationToken cancellationToken = default)
    {
        if (vitality is < 0 or > MaxDebugVitality)
            throw new DomainException($"Vitality must be between 0 and {MaxDebugVitality}.");

        if (guard is < 0 or > MaxDebugGuard)
            throw new DomainException($"Guard must be between 0 and {MaxDebugGuard}.");

        var run = await GetRunAsync(runId, cancellationToken);
        var combat = GetActiveCombat(run);
        var combatant = combat.Allies.Concat(combat.Enemies)
            .SingleOrDefault(combatant => combatant.Id.Value == combatantId)
            ?? throw new NotFoundException("Combatant", combatantId);

        combatant.DebugSetVitals(vitality, guard);
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsCombatDebugResult("Combatant vitals updated.", CombatRuntimeDto.FromDomain(combat));
    }

    public async Task<DevToolsCombatDebugResult> ApplyCombatantStatusAsync(
        Guid runId,
        Guid combatantId,
        string statusKey,
        int stacks,
        int durationTicks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
            throw new DomainException("Status key is required.");

        var run = await GetRunAsync(runId, cancellationToken);
        var combat = GetActiveCombat(run);
        var combatant = combat.Allies.Concat(combat.Enemies)
            .SingleOrDefault(c => c.Id.Value == combatantId)
            ?? throw new NotFoundException("Combatant", combatantId);

        combatant.ApplyStatusEffect(BuildDebugStatus(statusKey, stacks, durationTicks, combat.CurrentTick));
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new DevToolsCombatDebugResult($"Status '{statusKey}' applied.", CombatRuntimeDto.FromDomain(combat));
    }

    // Presets keyed by statusKey so devtools can apply realistic effects by name.
    // Durations/intervals are in ATB ticks (threshold = 50000; ~200-340 ticks/clock).
    private static CombatStatusEffect BuildDebugStatus(string statusKey, int stacks, int durationTicks, int currentTick)
    {
        var key = statusKey.Trim().ToLowerInvariant();
        // The devtools UI sends a coarse 1-99 "duration"; scale each unit to ~1000
        // ATB ticks so effects last a meaningful amount of real time.
        var dur = durationTicks > 0 ? durationTicks * 1000 : 6000;
        var s = Math.Max(1, stacks);

        return key switch
        {
            "poison" => CombatStatusEffect.Create("poison", "Poison", StatusEffectKind.DamageOverTime, currentTick, dur, magnitude: 8, stacks: s, tickInterval: AtbConstants.TicksPerTurn),
            "burn" => CombatStatusEffect.Create("burn", "Brûlure", StatusEffectKind.DamageOverTime, currentTick, dur, magnitude: 12, stacks: s, tickInterval: 3000),
            "regen" => CombatStatusEffect.Create("regen", "Régénération", StatusEffectKind.HealOverTime, currentTick, dur, magnitude: 10, stacks: s, tickInterval: AtbConstants.TicksPerTurn),
            "atk-up" => CombatStatusEffect.Create("atk-up", "Attaque +", StatusEffectKind.StatModifier, currentTick, dur, magnitude: 8, stacks: s, stat: CombatStat.AttackPower),
            "atk-down" => CombatStatusEffect.Create("atk-down", "Attaque −", StatusEffectKind.StatModifier, currentTick, dur, magnitude: -8, stacks: s, stat: CombatStat.AttackPower),
            "def-up" => CombatStatusEffect.Create("def-up", "Défense +", StatusEffectKind.StatModifier, currentTick, dur, magnitude: 8, stacks: s, stat: CombatStat.Defense),
            "def-down" => CombatStatusEffect.Create("def-down", "Défense −", StatusEffectKind.StatModifier, currentTick, dur, magnitude: -8, stacks: s, stat: CombatStat.Defense),
            "stun" => CombatStatusEffect.Create("stun", "Étourdi", StatusEffectKind.Stun, currentTick, dur, stacks: s),
            "silence" => CombatStatusEffect.Create("silence", "Silence", StatusEffectKind.Silence, currentTick, dur, stacks: s),
            "atb-lock" => CombatStatusEffect.Create("atb-lock", "Jauge bloquée", StatusEffectKind.AtbLock, currentTick, dur, stacks: s),
            _ => throw new DomainException(
                $"Unknown debug status '{statusKey}'. Try: poison, burn, regen, atk-up, atk-down, def-up, def-down, stun, silence, atb-lock.")
        };
    }

    private async Task<Run> GetRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        return await _runRepository.GetByIdAsync(new RunId(runId), cancellationToken)
            ?? throw new NotFoundException("Run", runId);
    }

    private static Combat GetActiveCombat(Run run)
    {
        return run.ActiveCombat ?? throw new DomainException("Run has no active combat.");
    }

    private static bool IsClosed(Run run)
    {
        return run.Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended;
    }

    private static void ClearCurrentRoomClimate(Run run)
    {
        var now = DateTime.UtcNow;
        foreach (var modifier in run.RunModifiers.Where(modifier =>
            modifier.Type == RunModifierType.RoomClimate &&
            modifier.ExpiresAtRoomId == run.CurrentRoomId.Value &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }
    }

    private static double? MapClimate(string? climate)
    {
        return climate?.Trim().ToLowerInvariant() switch
        {
            "none" => null,
            "grey" => 1,
            "rain" => 2,
            "heatwave" => 3,
            "hail" => 4,
            _ => throw new DomainException($"Unsupported room climate '{climate}'.")
        };
    }

}
