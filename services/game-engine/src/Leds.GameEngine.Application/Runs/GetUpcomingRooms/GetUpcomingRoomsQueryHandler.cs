using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetUpcomingRooms;

public sealed class GetUpcomingRoomsQueryHandler
    : IRequestHandler<GetUpcomingRoomsQuery, GetUpcomingRoomsResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IRunGenerator _runGenerator;

    public GetUpcomingRoomsQueryHandler(
        IRunRepository runRepository,
        IRunGenerator runGenerator)
    {
        _runRepository = runRepository;
        _runGenerator = runGenerator;
    }

    public async Task<GetUpcomingRoomsResponse> Handle(
        GetUpcomingRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        var isRevealed = run.RunModifiers.Any(modifier =>
            modifier.Type == RunModifierType.UpcomingRoomNamesRevealEnabled && !modifier.IsConsumed);

        if (!isRevealed)
        {
            return new GetUpcomingRoomsResponse(run.Id.Value, IsRevealed: false, []);
        }

        var preview = await _runGenerator.PreviewUpcomingRoomNamesAsync(run, cancellationToken);

        var rooms = preview
            .Select(room => new UpcomingRoomDto(room.RoomIndex, room.Key, room.DisplayName))
            .ToArray();

        return new GetUpcomingRoomsResponse(run.Id.Value, IsRevealed: true, rooms);
    }
}
