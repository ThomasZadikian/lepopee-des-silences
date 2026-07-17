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

        var oubliPartielPayoutDue = run.MoveToNextRoom(nextRoom);

        // "Loi de l'Oubli Partiel" (law.oubli-partiel): the floor just ended while the
        // forgotten-skill modifier was still active — pay out the compensation now that
        // the team "learns the lesson."
        if (oubliPartielPayoutDue)
        {
            await _playerProfileGateway.AwardStatPointsAsync(
                run.PlayerId, Run.SkillForgottenFloorEndStatPoints, cancellationToken);
        }

        // Ambient promulgation ("irréfusabilité") replaces the old player-chosen "Loi" map
        // node: a law may now be promulgated automatically on entering the new room.
        await _palaceLawPromulgator.PromulgateForRoomTransitionAsync(run, nextRoom, cancellationToken);

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