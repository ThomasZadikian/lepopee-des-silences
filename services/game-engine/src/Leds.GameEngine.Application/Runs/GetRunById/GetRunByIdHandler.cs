using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.PalaceIndicators;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunById;

public sealed class GetRunByIdQueryHandler : IRequestHandler<GetRunByIdQuery, GetRunByIdResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IPalaceIndicatorRepository _palaceIndicatorRepository;
    private readonly IPalacePublicIndicatorProjectionService _palacePublicIndicatorProjectionService;

    public GetRunByIdQueryHandler(
        IRunRepository runRepository,
        IPalaceIndicatorRepository palaceIndicatorRepository,
        IPalacePublicIndicatorProjectionService palacePublicIndicatorProjectionService)
    {
        _runRepository = runRepository;
        _palaceIndicatorRepository = palaceIndicatorRepository;
        _palacePublicIndicatorProjectionService = palacePublicIndicatorProjectionService;
    }

    public async Task<GetRunByIdResponse> Handle(
        GetRunByIdQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        var palaceIndicators = await _palaceIndicatorRepository.GetByRunIdAsync(
            request.RunId,
            cancellationToken);

        var publicIndicators = _palacePublicIndicatorProjectionService.Project(
            run,
            palaceIndicators);

        return new GetRunByIdResponse(RunDto.FromDomain(
            run,
            projectedPalaceIndicators: publicIndicators));
    }
}
