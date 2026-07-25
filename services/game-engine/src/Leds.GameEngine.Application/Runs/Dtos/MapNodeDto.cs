using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record MapNodeDto(
    Guid Id,
    string Type,
    int Row,
    int Lane,
    int RiskLevel,
    string? CombatRiskTier,
    string RewardProfile,
    IReadOnlyCollection<Guid> ParentNodeIds,
    string State,
    bool IsBoss,
    bool IsInitial,
    bool HasChosenEventOption,
    // How this node reads on the board: what walking onto it does, and what warning it gives
    // off beforehand. A hidden node never reaches the client at all (see RoomDto.FromDomain),
    // so there is no HiddenState here — its absence from the payload IS the hiding.
    string ContactBehavior,
    string DangerTell)
{
    public static MapNodeDto FromDomain(MapNode node)
    {
        return new MapNodeDto(
            node.Id.Value,
            node.EventType.ToString(),
            node.Row,
            node.Lane,
            node.RiskLevel,
            node.CombatRiskTier?.ToString(),
            node.RewardProfile,
            node.ParentNodeIds
                .Select(parentId => parentId.Value)
                .ToArray(),
            node.State.ToString(),
            node.IsBoss,
            node.IsInitial,
            node.HasChosenEventOption,
            node.ContactBehavior.ToString(),
            node.DangerTell.ToString());
    }
}