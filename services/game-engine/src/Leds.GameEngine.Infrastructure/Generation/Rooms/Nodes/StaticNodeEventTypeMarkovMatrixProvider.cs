using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class StaticNodeEventTypeMarkovMatrixProvider : INodeEventTypeMarkovMatrixProvider
{
    public const string MatrixKey = "node-event-type-transition";
    public const string SupportedVersion = "markov-node-event-type-0.1.0";

    public MarkovTransitionMatrix GetMatrix(string matrixVersion)
    {
        if (!string.Equals(matrixVersion, SupportedVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported node event type Markov matrix version '{matrixVersion}'.");
        }

        var combat = ToState(NodeEventType.Combat);
        var elite = ToState(NodeEventType.Elite);
        var item = ToState(NodeEventType.Item);
        var npc = ToState(NodeEventType.Npc);
        var rest = ToState(NodeEventType.Rest);
        var merchant = ToState(NodeEventType.Merchant);
        var law = ToState(NodeEventType.Law);
        var curse = ToState(NodeEventType.Curse);
        var rare = ToState(NodeEventType.Rare);

        var states = new[]
        {
            combat,
            elite,
            item,
            npc,
            rest,
            merchant,
            law,
            curse,
            rare
        };

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                combat,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.35m,
                    [elite] = 0.10m,
                    [item] = 0.15m,
                    [npc] = 0.10m,
                    [rest] = 0.05m,
                    [merchant] = 0.05m,
                    [law] = 0.05m,
                    [curse] = 0.05m,
                    [rare] = 0.10m
                }),

            MarkovTransitionRow.Create(
                elite,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.35m,
                    [elite] = 0.05m,
                    [item] = 0.15m,
                    [npc] = 0.05m,
                    [rest] = 0.10m,
                    [merchant] = 0.05m,
                    [law] = 0.10m,
                    [curse] = 0.05m,
                    [rare] = 0.10m
                }),

            MarkovTransitionRow.Create(
                item,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.25m,
                    [elite] = 0.05m,
                    [item] = 0.25m,
                    [npc] = 0.10m,
                    [rest] = 0.10m,
                    [merchant] = 0.10m,
                    [law] = 0.05m,
                    [curse] = 0.05m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                npc,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.20m,
                    [elite] = 0.05m,
                    [item] = 0.15m,
                    [npc] = 0.25m,
                    [rest] = 0.05m,
                    [merchant] = 0.10m,
                    [law] = 0.10m,
                    [curse] = 0.05m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                rest,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.30m,
                    [elite] = 0.05m,
                    [item] = 0.20m,
                    [npc] = 0.10m,
                    [rest] = 0.05m,
                    [merchant] = 0.15m,
                    [law] = 0.05m,
                    [curse] = 0.05m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                merchant,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.25m,
                    [elite] = 0.05m,
                    [item] = 0.25m,
                    [npc] = 0.05m,
                    [rest] = 0.10m,
                    [merchant] = 0.15m,
                    [law] = 0.05m,
                    [curse] = 0.05m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                law,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.20m,
                    [elite] = 0.05m,
                    [item] = 0.10m,
                    [npc] = 0.10m,
                    [rest] = 0.05m,
                    [merchant] = 0.05m,
                    [law] = 0.30m,
                    [curse] = 0.10m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                curse,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.30m,
                    [elite] = 0.15m,
                    [item] = 0.10m,
                    [npc] = 0.05m,
                    [rest] = 0.05m,
                    [merchant] = 0.05m,
                    [law] = 0.10m,
                    [curse] = 0.15m,
                    [rare] = 0.05m
                }),

            MarkovTransitionRow.Create(
                rare,
                new Dictionary<MarkovState, decimal>
                {
                    [combat] = 0.20m,
                    [elite] = 0.10m,
                    [item] = 0.15m,
                    [npc] = 0.10m,
                    [rest] = 0.05m,
                    [merchant] = 0.05m,
                    [law] = 0.15m,
                    [curse] = 0.05m,
                    [rare] = 0.15m
                })
        };

        return MarkovTransitionMatrix.Create(
            MatrixKey,
            SupportedVersion,
            states,
            rows);
    }

    private static MarkovState ToState(NodeEventType eventType)
    {
        return MarkovState.Create(eventType.ToString());
    }
}