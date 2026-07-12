using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Leds.GameEngine.Application.Runs.ExitMidRoom;

public sealed class ExitMidRoomCommandHandler
    : IRequestHandler<ExitMidRoomCommand, ExitMidRoomResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly IClock _clock;
    private readonly ILogger<ExitMidRoomCommandHandler> _logger;

    public ExitMidRoomCommandHandler(
        IRunRepository runRepository,
        IPlayerProfileGateway playerProfileGateway,
        IClock clock,
        ILogger<ExitMidRoomCommandHandler> logger)
    {
        _runRepository = runRepository;
        _playerProfileGateway = playerProfileGateway;
        _clock = clock;
        _logger = logger;
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

        // Same rationale as SaveAndExitRunCommandHandler: this suspends the run without firing
        // any integration event, so sync reputation now rather than risk it being orphaned.
        try
        {
            var scores = NpcReputationScoreMapper.ToScoreViews(run.NpcRelationships);
            if (scores.Count > 0)
            {
                await _playerProfileGateway.UpsertNpcReputationScoresAsync(
                    run.PlayerId, run.Id.Value, scores, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync NPC reputation for player {PlayerId} on exit-mid-room", run.PlayerId);
        }

        return new ExitMidRoomResponse(RunDto.FromDomain(run));
    }
}