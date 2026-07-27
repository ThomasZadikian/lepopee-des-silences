using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats.Tactical;

public interface ITacticalCombatFactory
{
    /// <summary>
    /// Pose un roster déjà constitué sur le terrain de <paramref name="room"/> et le déploie.
    /// </summary>
    TacticalCombat CreateFromRoster(
        CombatId combatId,
        CombatRoster roster,
        Room room,
        NodeId nodeId,
        RunId runId,
        DateTime createdAtUtc);
}
