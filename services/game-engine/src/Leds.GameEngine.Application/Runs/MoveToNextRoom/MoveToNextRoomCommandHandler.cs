using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveToNextRoom;

public sealed class MoveToNextRoomCommandHandler
    : IRequestHandler<MoveToNextRoomCommand, MoveToNextRoomResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRunGenerator _runGenerator;
    private readonly IAmbientPalaceLawPromulgator _palaceLawPromulgator;

    public MoveToNextRoomCommandHandler(
        IRunRepository runRepository,
        IRunGenerator runGenerator,
        IAmbientPalaceLawPromulgator palaceLawPromulgator)
    {
        _runRepository = runRepository;
        _runGenerator = runGenerator;
        _palaceLawPromulgator = palaceLawPromulgator;
    }

    public async Task<MoveToNextRoomResponse> Handle(
        MoveToNextRoomCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot enter the next room: run must be in Interlude state.");
        }

        var nextRoom = await _runGenerator.GenerateNextRoomAsync(run, cancellationToken);

        run.MoveToNextRoom(nextRoom);

        // Ambient promulgation ("irréfusabilité") replaces the old player-chosen "Loi" map
        // node: a law may now be promulgated automatically on entering the new room.
        await _palaceLawPromulgator.PromulgateForRoomTransitionAsync(run, nextRoom, cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new MoveToNextRoomResponse(RunDto.FromDomain(run));
    }
}