using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandHandler
    : IRequestHandler<SaveAndExitRunCommand, SaveAndExitRunResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;

    public SaveAndExitRunCommandHandler(
        IRunRepository runRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _clock = clock;
    }

    public async Task<SaveAndExitRunResponse> Handle(
        SaveAndExitRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.SaveAndExit(_clock.UtcNow);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SaveAndExitRunResponse(RunDto.FromDomain(run));
    }
}
