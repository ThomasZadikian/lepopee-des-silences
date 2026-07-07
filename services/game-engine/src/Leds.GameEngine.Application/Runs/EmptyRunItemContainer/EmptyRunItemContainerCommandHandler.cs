using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.EmptyRunItemContainer;

public sealed class EmptyRunItemContainerCommandHandler
    : IRequestHandler<EmptyRunItemContainerCommand, EmptyRunItemContainerResponse>
{
    private readonly IRunRepository _runRepository;

    public EmptyRunItemContainerCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<EmptyRunItemContainerResponse> Handle(
        EmptyRunItemContainerCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);
        var containerId = new RunItemId(request.ContainerItemId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.EmptyContainer(containerId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new EmptyRunItemContainerResponse(
            run.Id.Value,
            containerId.Value,
            run.RunItems.Select(RunItemDto.FromDomain).ToArray());
    }
}
