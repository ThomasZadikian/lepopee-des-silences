using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Leds.GameEngine.Application.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandHandler
    : IRequestHandler<SaveAndExitRunCommand, SaveAndExitRunResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly IClock _clock;
    private readonly ILogger<SaveAndExitRunCommandHandler> _logger;

    public SaveAndExitRunCommandHandler(
        IRunRepository runRepository,
        IPlayerProfileGateway playerProfileGateway,
        IClock clock,
        ILogger<SaveAndExitRunCommandHandler> logger)
    {
        _runRepository = runRepository;
        _playerProfileGateway = playerProfileGateway;
        _clock = clock;
        _logger = logger;
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

        // A suspended run can stay unresumed indefinitely — or get silently orphaned if the
        // player starts a fresh run instead of resuming — and unlike Abandon/CombatDefeat, no
        // integration event fires for a mere pause. Sync reputation gained so far right away so
        // it survives either way. Must never block or fail the save itself.
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
            _logger.LogWarning(ex, "Failed to sync NPC reputation for player {PlayerId} on save-and-exit", run.PlayerId);
        }

        return new SaveAndExitRunResponse(RunDto.FromDomain(run));
    }
}