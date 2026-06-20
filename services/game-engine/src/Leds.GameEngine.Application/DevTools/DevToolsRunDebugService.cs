using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.DevTools;

public sealed class DevToolsRunDebugService : IDevToolsRunDebugService
{
    private const int MaxAdvanceRoomCount = 10;
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

        run.ActivatePalaceLaw(ToDomainLaw(definitionResult.Value));

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
        var rewardOffer = _combatResolution.ApplyOutcome(run, combat, DateTimeOffset.UtcNow);
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
        var rewardOffer = _combatResolution.ApplyOutcome(run, combat, DateTimeOffset.UtcNow);
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

    private static PalaceLaw ToDomainLaw(PalaceLawDefinitionSnapshot definition)
    {
        var domains = definition.ImpactDomains
            .Select(domain => Enum.TryParse<PalaceLawDomain>(domain, ignoreCase: true, out var parsed)
                ? parsed
                : (PalaceLawDomain?)null)
            .Where(domain => domain.HasValue)
            .Select(domain => domain!.Value)
            .Distinct()
            .ToArray();

        if (domains.Length == 0)
            domains = [PalaceLawDomain.Combat];

        var effects = (definition.Effects ?? [])
            .Select(MapLawEffect)
            .Where(effect => effect is not null)
            .Select(effect => effect!)
            .ToArray();

        return PalaceLaw.Create(definition.Key, definition.Name, definition.Version, domains, effects);
    }

    private static PalaceLawEffect? MapLawEffect(CatalogEffectDefinitionSnapshot effect)
    {
        if (!Enum.TryParse<EffectType>(effect.EffectType, ignoreCase: true, out var effectType))
            throw new DomainException($"Palace law effect type '{effect.EffectType}' is not supported by the runtime.");

        var duration = Enum.TryParse<RunModifierDuration>(effect.Duration, ignoreCase: true, out var parsedDuration)
            ? parsedDuration
            : RunModifierDuration.UntilRunEnds;

        return effectType switch
        {
            EffectType.AddStartingGuard => PalaceLawEffect.Create(
                RunModifierType.StartingGuardBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifyDifficultyMultiplier => PalaceLawEffect.Create(
                RunModifierType.CombatDifficultyMultiplier,
                (double)effect.Value,
                duration),
            EffectType.ModifyRewardPowerMultiplier => PalaceLawEffect.Create(
                RunModifierType.RewardPowerMultiplier,
                (double)effect.Value,
                duration),
            EffectType.ModifyAttackPower => PalaceLawEffect.Create(
                RunModifierType.AttackPowerBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifyDefense => PalaceLawEffect.Create(
                RunModifierType.DefenseBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifySpeed => PalaceLawEffect.Create(
                RunModifierType.SpeedBonus,
                (double)effect.Value,
                duration),
            EffectType.ApplyRoomClimate => PalaceLawEffect.Create(
                RunModifierType.RoomClimate,
                MapClimate(effect.Condition ?? effect.BehaviorTag)
                    ?? throw new DomainException("ApplyRoomClimate requires a concrete climate."),
                RunModifierDuration.UntilRoomEnds),
            EffectType.ModifyGenerationWeight => null,
            _ => throw new DomainException($"Palace law effect type '{effect.EffectType}' is not supported by the runtime.")
        };
    }
}
