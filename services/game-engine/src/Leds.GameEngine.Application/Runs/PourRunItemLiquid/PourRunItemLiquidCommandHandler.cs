using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.PourRunItemLiquid;

public sealed class PourRunItemLiquidCommandHandler
    : IRequestHandler<PourRunItemLiquidCommand, PourRunItemLiquidResponse>
{
    private readonly IRunRepository _runRepository;

    public PourRunItemLiquidCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<PourRunItemLiquidResponse> Handle(
        PourRunItemLiquidCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);
        var containerId = new RunItemId(request.ContainerItemId);
        var liquidItemId = new RunItemId(request.LiquidItemId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var depleted = run.PourLiquid(containerId, liquidItemId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        var container = run.RunItems.First(i => i.Id == containerId);

        return new PourRunItemLiquidResponse(
            run.Id.Value,
            container.Id.Value,
            container.ContainedLiquidDefinitionKey!,
            depleted,
            run.RunItems.Select(RunItemDto.FromDomain).ToArray());
    }
}
