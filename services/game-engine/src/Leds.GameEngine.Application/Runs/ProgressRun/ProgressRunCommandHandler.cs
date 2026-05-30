using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ProgressRun;

public sealed class ProgressRunCommandHandler
    : IRequestHandler<ProgressRunCommand, ProgressRunResponse>
{
    private readonly IRunRepository _runRepository;

    public ProgressRunCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ProgressRunResponse> Handle(
        ProgressRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.ProgressCurrentRoom();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ProgressRunResponse(RunDto.FromDomain(run));
    }
}