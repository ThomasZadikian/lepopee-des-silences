using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ExitMidRoom;

public sealed class ExitMidRoomCommandHandler
    : IRequestHandler<ExitMidRoomCommand, ExitMidRoomResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;

    public ExitMidRoomCommandHandler(IRunRepository runRepository, IClock clock)
    {
        _runRepository = runRepository;
        _clock = clock;
    }

    public async Task<ExitMidRoomResponse> Handle(
        ExitMidRoomCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        run.ExitMidRoom(_clock.UtcNow);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ExitMidRoomResponse(RunDto.FromDomain(run));
    }
}
