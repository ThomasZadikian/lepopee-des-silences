using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ResolveSelectedNode;

public sealed class ResolveSelectedNodeCommandHandler
    : IRequestHandler<ResolveSelectedNodeCommand, ResolveSelectedNodeResponse>
{
    private readonly IRunRepository _runRepository;

    public ResolveSelectedNodeCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ResolveSelectedNodeResponse> Handle(
        ResolveSelectedNodeCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.ResolveSelectedNode();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ResolveSelectedNodeResponse(RunDto.FromDomain(run));
    }
}