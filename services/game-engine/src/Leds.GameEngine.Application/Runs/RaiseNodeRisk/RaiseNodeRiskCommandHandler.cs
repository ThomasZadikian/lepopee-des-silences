using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.RaiseNodeRisk;

public sealed class RaiseNodeRiskCommandHandler
    : IRequestHandler<RaiseNodeRiskCommand, RaiseNodeRiskResponse>
{
    private readonly IRunRepository _runRepository;

    public RaiseNodeRiskCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<RaiseNodeRiskResponse> Handle(
        RaiseNodeRiskCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.RaiseNodeRisk(request.NodeId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new RaiseNodeRiskResponse(RunDto.FromDomain(run));
    }
}
