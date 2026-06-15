using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetCurrentCombat;

public sealed class GetCurrentCombatQueryHandler
    : IRequestHandler<GetCurrentCombatQuery, CombatRuntimeDto>
{
    private readonly IRunRepository _runRepository;

    public GetCurrentCombatQueryHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<CombatRuntimeDto> Handle(
        GetCurrentCombatQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.ActiveCombat is null)
        {
            throw new NotFoundException($"No active combat was found for run '{request.RunId}'.");
        }

        return CombatRuntimeDto.FromDomain(run.ActiveCombat, CombatItemHelper.GetUsableBattleItems(run));
    }
}