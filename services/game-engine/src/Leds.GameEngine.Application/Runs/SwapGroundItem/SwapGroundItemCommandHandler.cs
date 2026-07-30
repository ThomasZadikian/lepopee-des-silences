using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SwapGroundItem;

public sealed class SwapGroundItemCommandHandler
    : IRequestHandler<SwapGroundItemCommand, SwapGroundItemResponse>
{
    private readonly IRunRepository _runRepository;

    public SwapGroundItemCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<SwapGroundItemResponse> Handle(
        SwapGroundItemCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.SwapGroundItemIntoInventory(
            new RunItemId(request.GroundItemId),
            new RunItemId(request.HeldItemId));

        await _runRepository.UpdateAsync(run, cancellationToken);
        return new SwapGroundItemResponse(RunDto.FromDomain(run));
    }
}
