using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ChooseNode;

public sealed class ChooseNodeCommandHandler : IRequestHandler<ChooseNodeCommand, ChooseNodeResponse>
{
    private readonly IRunRepository _runRepository;

    public ChooseNodeCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ChooseNodeResponse> Handle(
        ChooseNodeCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);
        var nodeId = new NodeId(request.NodeId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.ChooseNode(nodeId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ChooseNodeResponse(RunDto.FromDomain(run));
    }
}