using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveParty;

public sealed class MovePartyCommandHandler : IRequestHandler<MovePartyCommand, MovePartyResponse>
{
    private readonly IRunRepository _runRepository;

    public MovePartyCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<MovePartyResponse> Handle(
        MovePartyCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var pickups = run.MoveParty(request.TargetX, request.TargetY);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new MovePartyResponse(
            RunDto.FromDomain(run),
            pickups.CollectedItemIds.Select(id => id.Value).ToArray(),
            pickups.BlockedItemIds.Select(id => id.Value).ToArray());
    }
}
