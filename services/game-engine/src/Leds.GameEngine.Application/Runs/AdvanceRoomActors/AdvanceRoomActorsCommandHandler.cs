using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.AdvanceRoomActors;

public sealed class AdvanceRoomActorsCommandHandler
    : IRequestHandler<AdvanceRoomActorsCommand, AdvanceRoomActorsResponse>
{
    private readonly IRunRepository _runRepository;

    public AdvanceRoomActorsCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<AdvanceRoomActorsResponse> Handle(
        AdvanceRoomActorsCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var result = run.AdvanceRoomActors(request.Mode);
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new AdvanceRoomActorsResponse(
            RunDto.FromDomain(run),
            result.Movements.Select(movement => new ActorMovementDto(
                movement.ActorId,
                movement.ActorKind.ToString(),
                movement.FromX,
                movement.FromY,
                movement.ToX,
                movement.ToY)).ToArray(),
            result.TriggeredNodeId?.Value);
    }
}
