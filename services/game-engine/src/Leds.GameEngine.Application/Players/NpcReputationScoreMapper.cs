using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Application.Players;

public static class NpcReputationScoreMapper
{
    public static IReadOnlyCollection<NpcReputationScoreView> ToScoreViews(
        IReadOnlyCollection<NpcRelationship> relationships)
    {
        return relationships
            .Where(r => r.TimesMet > 0 || r.RelationshipScore != 0)
            .Select(r => new NpcReputationScoreView(r.NpcKey, r.RelationshipScore, r.TimesMet, r.CurrentDialogueNodeKey))
            .ToArray();
    }
}
