using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.AbandonRun;

public sealed class AbandonRunCommandHandler
    : IRequestHandler<AbandonRunCommand, AbandonRunResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;

    public AbandonRunCommandHandler(
        IRunRepository runRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _clock = clock;
    }

    public async Task<AbandonRunResponse> Handle(
        AbandonRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status is not (RunStatus.RoomResolved or RunStatus.Interlude))
        {
            throw new DomainException(
                "AbandonRun is only allowed from a safe point (RoomResolved or Interlude).");
        }

        run.Abandon(_clock.UtcNow);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new AbandonRunRes