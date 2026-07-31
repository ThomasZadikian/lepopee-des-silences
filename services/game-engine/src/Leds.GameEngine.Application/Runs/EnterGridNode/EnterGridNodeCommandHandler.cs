using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.EnterGridNode;

public sealed class EnterGridNodeCommandHandler : IRequestHandler<EnterGridNodeCommand, EnterGridNodeResponse>
{
    private readonly IRunRepository _runRepository;

    public EnterGridNodeCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<EnterGridNodeResponse> Handle(
        EnterGridNodeCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        if (run.Status == RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot enter a node: run is in Interlude. Navigate the interlude hub or enter the next room.");
        }

        run.EnterGridNode(request.NodeId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new EnterGridNodeResponse(RunDto.FromDomain(run));
    }
}
