using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveToNextRoom;

public sealed class MoveToNextRoomCommandHandler
    : IRequestHandler<MoveToNextRoomCommand, MoveToNextRoomResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRunGenerator _runGenerator;
    private readonly IAmbientPalaceLawPromulgator _palaceLawPromulgator;
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public MoveToNextRoomCommandHandler(
        IRunRepository runRepository,
        IRunGenerator runGenerator,
        IAmbientPalaceLawPromulgator palaceLawPromulgator,
        IPlayerProfileGateway playerProfileGateway)
    {
        _runRepository = runRepository;
        _runGenerator = runGenerator;
        _palaceLawPromulgator = palaceLawPromulgator;
        _playerProfileGateway = playerProfileGateway;
    }

    public async Task<MoveToNextRoomResponse> Handle(
        MoveToNextRoomCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot enter the next room: run must be in Interlude state.");
        }

        var leftRoom = run.CurrentRoom;
        var nextRoom = await _runGenerator.GenerateNextRoomAsync(run, cancellationToken);

        var floorEndResult = run.MoveToNextRoom(nextRoom);

        // "Loi de l'Oubli Partiel" (law.oubli-partiel): the floor just ended while the
        // forgotten-skill modifier was still active — pay out the compensation now that
        // the team "learns the lesson."
        if (floorEndResult.OubliPartielPayoutDue)
        {
            await _playerProfileGateway.AwardStatPointsAsync(
                run.PlayerId, Run.SkillForgottenFloorEndStatPoints, cancellationToken);
        }

        // "Loi du Prêteur" (law.preteur): the floor just ended while the currency-gain
        // bonus was still active — the Palais claws back a fraction of the current
        // total, interest included.
        if (floorEndResult.PreteurClawbackDue)
        {
            var profile = await _playerProfileGateway.GetProfileAsync(run.PlayerId, cancellationToken);
            var clawbackAmount = (int)Math.Floor(
                profile.Progression.PalaceShardCount * Run.PreteurClawbackFraction);

            if (clawbackAmount > 0)
            {
                await _playerProfileGateway.TrySpendCurrencyAsync(run.PlayerId, clawbackAmount, cancellationToken);
            }
        }

        // Ambient promulgation ("irréfusabilité") replaces the old player-chosen "Loi" map
        // node: a law may now be promulgated automatically on entering the new room.
        await _palaceLawPromulgator.PromulgateForRoomTransitionAsync(run, nextRoom, cancellationToken);

        // "Loi de l'Impôt du Seuil" (law.impot-seuil): a toll is charged at the entry of
        // the new room, once the room's promulgated law state above is finalized.
        // Insolvency applies a stacking max-HP debuff instead of blocking the transition
        // (the SFD's "le Palais prélève en nature").
        var roomTollAmount = (int)run.RunModifiers
            .Where(m => m.Type == RunModifierType.RoomTollAmount && !m.IsConsumed)
            .Sum(m => m.Value);

        if (roomTollAmount > 0)
        {
            var paid = await _playerProfileGateway.TrySpendCurrencyAsync(
                run.PlayerId, roomTollAmount, cancellationToken);

            if (!paid)
            {
                run.ApplyRoomTollInsolvencyDebuff();
            }
        }

        // Dévoration: chip HP away when the room just left had no combat resolution at all.
        if (!HasResolvedCombatNode(leftRoom))
        {
            run.OnRoomEnteredWithoutCombat();
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new MoveToNextRoomResponse(RunDto.FromDomain(run));
    }

    private static bool HasResolvedCombatNode(Room room)
    {
        return room.Nodes.Any(node =>
            node.State == NodeState.Resolved &&
            node.EventType is NodeEventType.Combat or NodeEventType.Elite
                or NodeEventType.RoomBoss or NodeEventType.FinalBoss);
    }
}