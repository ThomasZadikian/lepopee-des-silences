using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunInventory;

public sealed class GetRunInventoryQueryHandler
    : IRequestHandler<GetRunInventoryQuery, GetRunInventoryResponse>
{
    private readonly IRunRepository _runRepository;

    public GetRunInventoryQueryHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<GetRunInventoryResponse> Handle(
        GetRunInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        var items = run.RunItems
            .Select(RunItemDto.FromDomain)
            .ToArray();

        return new GetRunInventoryResponse(run.Id.Value, items);
    }
}