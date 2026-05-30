using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandler
    : IRequestHandler<ResolveCurrentEventCommand, ResolveCurrentEventResponse>
{
    private readonly IRunRepository _runRepository;

    public ResolveCurrentEventCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ResolveCurrentEventResponse> Handle(
        ResolveCurrentEventCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.ResolveCurrentEvent();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ResolveCurrentEventResponse(RunDto.FromDomain(run));
    }
}