using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.IntegrationEvents;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
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

        if (run.Status is not (RunStatus.RoomResolved or RunStatus.Interlude))
        {
            throw new DomainException(
                "AbandonRun is only allowed from a safe point (RoomResolved or Interlude).");
        }

        run.Abandon(_clock.UtcNow);

        var evt = RunIntegrationEventFactory.CreateAbandoned(run, _clock.UtcNow.UtcDateTime);
        await _outboxWriter.WriteAsync(evt, cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new AbandonRunResponse(RunDto.FromDomain(run));
    }
}