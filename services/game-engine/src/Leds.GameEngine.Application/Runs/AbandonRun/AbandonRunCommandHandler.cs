using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.IntegrationEvents;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.AbandonRun;

public sealed class AbandonRunCommandHandler
    : IRequestHandler<AbandonRunCommand, AbandonRunResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;

    public AbandonRunCommandHandler(
        IRunRepository runRepository,
        IOutboxWriter outboxWriter,
        IClock clock)
    {
        _runRepository = runRepository;
        _outboxWriter = outboxWriter;
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

        // Abandonment is the recovery escape hatch for any open run. Unlike SaveAndExit it
        // deliberately discards in-progress combat/event state, so requiring a safe point here
        // can permanently lock an account after a browser reset or lost local run reference.
        run.Abandon(_clock.UtcNow);

        var evt = RunIntegrationEventFactory.CreateAbandoned(run, _clock.UtcNow.UtcDateTime);
        await _outboxWriter.WriteAsync(evt, cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new AbandonRunResponse(RunDto.FromDomain(run));
    }
}
