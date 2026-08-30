using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SyncPartyStats;

/// <summary>
/// Equipment mid-run resync: re-reads the current base stats and equipment from
/// player-service and re-applies StartRun's effective-stat computation (via
/// <see cref="PlayerStatMerger"/>) to every character already in this run's roster.
/// Mirrors <c>SyncPartySkillsCommandHandler</c>
/// exactly, kept as a separate command so the already-shipped skill resync isn't touched.
/// </summary>
public sealed class SyncPartyStatsCommandHandler
    : IRequestHandler<SyncPartyStatsCommand, SyncPartyStatsResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly PlayerSkillMerger _skillMerger;
    private readonly PlayerStatMerger _statMerger;

    public SyncPartyStatsCommandHandler(
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        PlayerSkillMerger skillMerger,
        PlayerStatMerger statMerger)
    {
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _skillMerger = skillMerger;
        _statMerger = statMerger;
    }

    public async Task<SyncPartyStatsResponse> Handle(
        SyncPartyStatsCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        if (run.HasActiveCombat)
        {
            throw new DomainException("Cannot sync party stats while a combat is active.");
        }

        var snapshot = await _playerGateway.GetRunSnapshotAsync(run.PlayerId, cancellationToken);

        for (var i = 0; i < snapshot.Characters.Count; i++)
        {
            var character = snapshot.Characters.ElementAt(i);
            var equipmentEffects = await _skillMerger.CollectEquippedItemEffectsAsync(
                character.EquippedItems, cancellationToken);
            var effectiveStats = _statMerger.ComputeEffectiveStats(character.Stats, equipmentEffects);

            if (i == 0)
            {
                run.ReplacePlayerStats(
                    maxVitality: effectiveStats.MaxVitality,
                    maxMana: effectiveStats.Mana,
                    charge: effectiveStats.Charge,
                    attack: effectiveStats.AttackPower,
                    defense: effectiveStats.Defense,
                    speed: effectiveStats.Speed,
                    focus: effectiveStats.Focus,
                    magicAttack: effectiveStats.MagicAttack,
                    magicDefense: effectiveStats.MagicDefense,
                    guardBonusPercent: effectiveStats.GuardBonusPercent);
            }

            var existingSnapshotCharacter = run.PlayerSnapshot?.Characters
                .FirstOrDefault(c => c.CharacterId == character.CharacterId);

            if (existingSnapshotCharacter is not null)
            {
                run.ReplaceCharacterStats(
                    character.CharacterId,
                    maxVitality: effectiveStats.MaxVitality,
                    attackPower: effectiveStats.AttackPower,
                    defense: effectiveStats.Defense,
                    startingGuard: character.Stats.StartingGuard,
                    speed: effectiveStats.Speed,
                    initiative: character.Stats.Initiative,
                    focus: effectiveStats.Focus,
                    mana: effectiveStats.Mana,
                    charge: effectiveStats.Charge,
                    magicAttack: effectiveStats.MagicAttack,
                    magicDefense: effectiveStats.MagicDefense,
                    movement: effectiveStats.Movement,
                    equippedItemKeys: character.EquippedItems);
            }
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SyncPartyStatsResponse(RunDto.FromDomain(run));
    }
}
