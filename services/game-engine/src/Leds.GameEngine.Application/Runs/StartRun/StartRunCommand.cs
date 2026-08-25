using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

/// <summary>
/// Starts the mandatory Story flow for an incomplete account, otherwise a selectable Difficulty N.
/// </summary>
public sealed record StartRunCommand(Guid PlayerId, int? DifficultyLevel = null) : IRequest<StartRunResponse>;
