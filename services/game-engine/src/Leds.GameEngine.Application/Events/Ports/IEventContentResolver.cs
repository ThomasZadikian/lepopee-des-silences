using Leds.GameEngine.Application.Events.Contracts;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Application.Events.Ports;

public interface IEventContentResolver
{
    Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default);
}