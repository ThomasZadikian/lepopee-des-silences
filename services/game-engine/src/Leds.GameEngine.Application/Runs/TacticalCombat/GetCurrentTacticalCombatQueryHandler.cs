using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

public sealed class GetCurrentTacticalCombatQueryHandler
    : IRequestHandler<GetCurrentTacticalCombatQuery, TacticalCombatRuntimeDto>
{
    private readonly IRunRepository _runRepository;

    public GetCurrentTacticalCombatQueryHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<TacticalCombatRuntimeDto> Handle(
        GetCurrentTacticalCombatQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(
            new RunId(request.RunId), cancellationToken);

        if (run is null)
            throw new NotFoundException("Run", request.RunId);

        if (run.ActiveTacticalCombat is null)
            throw new NotFoundException(
                $"No active tactical combat was found for run '{request.RunId}'.");

        return TacticalCombatRuntimeDto.FromDomain(
            run.ActiveTacticalCombat, CombatItemHelper.GetUsableBattleItems(run));
    }
}
