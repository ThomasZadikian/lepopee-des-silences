using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ChallengeBossRemotely;

public sealed class ChallengeBossRemotelyCommandHandler
    : IRequestHandler<ChallengeBossRemotelyCommand, ChallengeBossRemotelyResponse>
{
    private readonly IRunRepository _runRepository;

    public ChallengeBossRemotelyCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<ChallengeBossRemotelyResponse> Handle(
        ChallengeBossRemotelyCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.ChallengeBossRemotely();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ChallengeBossRemotelyResponse(RunDto.FromDomain(run));
    }
}
