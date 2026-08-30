using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SyncPartySkills;

/// <summary>
/// Grimoire "Valider les choix": re-reads the freshest equipped-skill selection from
/// player-service and re-applies StartRun's skill-merge logic (via <see cref="PlayerSkillMerger"/>)
/// to every character already in this run's roster, so a mid-run equip/unequip takes
/// effect in the next combat instead of only on the next Run. Both representations of
/// the protagonist are updated (<see cref="Run.PlayerState"/>, which actually drives
/// combat, and its <see cref="Run.PlayerSnapshot"/> entry) alongside every companion's
/// <see cref="RunCharacterSnapshot"/>.
/// </summary>
public sealed class SyncPartySkillsCommandHandler
    : IRequestHandler<SyncPartySkillsCommand, SyncPartySkillsResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly PlayerSkillMerger _skillMerger;

    public SyncPartySkillsCommandHandler(
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        PlayerSkillMerger skillMerger)
    {
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _skillMerger = skillMerger;
    }

    public async Task<SyncPartySkillsResponse> Handle(
        SyncPartySkillsCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        if (run.HasActiveCombat)
        {
            throw new DomainException("Cannot sync party skills while a combat is active.");
        }

        var snapshot = await _playerGateway.GetRunSnapshotAsync(run.PlayerId, cancellationToken);

        for (var i = 0; i < snapshot.Characters.Count; i++)
        {
            var character = snapshot.Characters.ElementAt(i);

            var equippedDefinitions = await _skillMerger.ResolveEquippedItemsAsync(
                character.EquippedItems, cancellationToken);
            var equipmentEffects = equippedDefinitions
                .SelectMany(item => item.EquipmentEffects ?? [])
                .ToArray();
            var weapon = equippedDefinitions.SingleOrDefault(item =>
                string.Equals(item.Category, "Weapon", StringComparison.OrdinalIgnoreCase));
            var mergedSkills = await _skillMerger.MergeSkillsAsync(
                character, equipmentEffects, cancellationToken, weapon);

            if (i == 0)
            {
                run.ReplacePlayerSkills(mergedSkills
                    .Select(s => PlayerRuntimeSkill.Create(
                        key: s.Key,
                        displayName: s.DisplayName,
                        skillType: s.SkillType,
                        targetingType: s.TargetingType,
                        effectType: s.EffectType,
                        manaCost: s.ManaCost,
                        chargeCost: s.ChargeCost,
                        basePower: s.BasePower,
                        category: s.Category,
                        basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                        tacticalRange: s.TacticalRange,
                        tacticalAreaShape: s.TacticalAreaShape,
                        requiresLineOfSight: s.RequiresLineOfSight,
                        cooldown: s.Cooldown,
                        isUltimate: s.IsUltimate,
                        emotionalRegister: s.EmotionalRegister))
                    .ToArray());
            }

            var existingSnapshotCharacter = run.PlayerSnapshot?.Characters
                .FirstOrDefault(c => c.CharacterId == character.CharacterId);

            if (existingSnapshotCharacter is not null)
            {
                run.ReplaceCharacterSkills(character.CharacterId, mergedSkills
                    .Select(s => RunCharacterSkillSnapshot.Create(
                        skillDefinitionKey: s.Key,
                        displayName: s.DisplayName,
                        skillType: s.SkillType,
                        targetingMode: s.TargetingType,
                        effectType: s.EffectType,
                        manaCost: s.ManaCost,
                        chargeCost: s.ChargeCost,
                        basePower: s.BasePower,
                        category: s.Category,
                        basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                        tacticalRange: s.TacticalRange,
                        tacticalAreaShape: s.TacticalAreaShape,
                        requiresLineOfSight: s.RequiresLineOfSight,
                        cooldown: s.Cooldown,
                        isUltimate: s.IsUltimate,
                        emotionalRegister: s.EmotionalRegister))
                    .ToArray());
            }
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SyncPartySkillsResponse(RunDto.FromDomain(run));
    }
}
