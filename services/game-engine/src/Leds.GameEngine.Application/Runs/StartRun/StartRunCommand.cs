using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

/// <summary>
/// Lance une run avec le système de combat tactique, désormais unique mode jouable.
/// </summary>
public sealed record StartRunCommand(Guid PlayerId) : IRequest<StartRunResponse>;
