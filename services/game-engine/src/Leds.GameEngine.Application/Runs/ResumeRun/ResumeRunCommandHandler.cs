using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ResumeRun;

public sealed class ResumeRunCommandHandler
    : IRequestHandler<ResumeRunCommand, ResumeRunResponse>
{
    private readonly IRunRepository _runRepository;

    public ResumeRunCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ResumeRunResponse> Handle(
        ResumeRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.Resume();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ResumeRunResponse(RunDto.FromDomain(run));
    }
}