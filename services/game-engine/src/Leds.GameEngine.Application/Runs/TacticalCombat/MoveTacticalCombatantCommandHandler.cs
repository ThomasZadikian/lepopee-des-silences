using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

public sealed class MoveTacticalCombatantCommandHandler
    : IRequestHandler<MoveTacticalCombatantCommand, TacticalCombatResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;

    public MoveTacticalCombatantCommandHandler(IRunRepository runRepository, IClock clock)
    {
        _runRepository = runRepository;
        _clock = clock;
    }

    public async Task<TacticalCombatResponse> Handle(
        MoveTacticalCombatantCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var combat = run.RequireActiveTacticalCombat();

        var actorId = combat.ActiveCombatantId;
        var destination = new GridPosition(request.TargetX, request.TargetY);

        // Le domaine valide portée, praticabilité et occupation, et renvoie le coût réel du
        // trajet — plus élevé qu'à vol d'oiseau dès qu'il faut monter.
        var cost = combat.MoveActiveCombatant(destination);

        await _runRepository.UpdateAsync(run, cancellationToken);

        var logEntry = new CombatLogEntryDto(
            OccurredAtUtc: _clock.UtcNow.UtcDateTime,
            Type: "TacticalMove",
            Message: $"Déplacement en ({destination.X}, {destination.Y}) pour {cost} de mouvement.",
            ActorId: actorId,
            SkillKey: null,
            TargetIds: []);

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            [logEntry]);
    }
}
