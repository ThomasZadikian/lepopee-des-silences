using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Application.Events.Resolution;

public interface IEventContentResolutionStrategy
{
    IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; }

    Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default);
}