using Leds.GameEngine.Domain.Interlude;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Interlude;

/// <summary>
/// Returns the set of interlude hub nodes to display between two rooms.
/// Implementations must return at minimum: Player, Elise, Inventory, Journal, Placeholder × 2.
/// </summary>
public interface IInterludeNodeProvider
{
    /// <summary>
    /// Returns the ordered list of interlude nodes for the given run.
    /// The run is provided so future implementations can adapt nodes
    /// (e.g., disable Elise if not yet unlocked).
    /// </summary>
    IReadOnlyCollection<InterludeNode> GetNodes(Run run);
}
