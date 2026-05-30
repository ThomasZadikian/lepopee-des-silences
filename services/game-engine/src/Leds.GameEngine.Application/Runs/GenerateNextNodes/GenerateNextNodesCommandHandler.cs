using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GenerateNextNodes;

public sealed class GenerateNextNodesCommandHandler
    : IRequestHandler<GenerateNextNodesCommand, GenerateNextNodesResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRunGenerator _runGenerator;

    public GenerateNextNodesCommandHandler(
        IRunRepository runRepository,
        IRunGenerator runGenerator)
    {
        _runRepository = runRepository;
        _runGenerator = runGenerator;
    }

    public async Task<GenerateNextNodesResponse> Handle(
        GenerateNextNodesCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        var nextNodes = _runGenerator.GenerateNextNodes(run);

        run.AddNextNodesToCurrentRoom(nextNodes);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new GenerateNextNodesResponse(RunDto.FromDomain(run));
    }
}