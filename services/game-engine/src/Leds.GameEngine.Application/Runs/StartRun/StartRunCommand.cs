using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

/// <param name="ExplorationMode">
/// Player's chosen combat/exploration system for the run — <c>"Classic"</c> (DAG node graph)
/// or <c>"Tactical"</c> (free-roam grid). Unlike the other flags on this command, this is an
/// explicit player choice supplied by the API caller, not derived from the player's profile.
/// Defaults to Classic when absent or unrecognized.
/// </param>
public sealed record StartRunCommand(Guid PlayerId, string? ExplorationMode = null)
    : IRequest<StartRunResponse>;