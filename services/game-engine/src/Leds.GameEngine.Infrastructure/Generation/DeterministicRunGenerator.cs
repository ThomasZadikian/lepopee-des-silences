using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    public string GeneratorVersion => "gen-0.1.0";

    public string MarkovMatrixVersion => "markov-0.1.0";

    public string GenerateSeed()
    {
        return $"seed-{Guid.NewGuid():N}";
    }

    public Room GenerateInitialRoom(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed is required.", nameof(seed));
        }

        var random = CreateDeterministicRandom($"{seed}:room:0");
        var maxNodeDepth = random.Next(2, 5);

        var nodes = new[]
        {
            CreateNode(random, NodeEventType.Combat, nodeDepth: 0),
            CreateNode(random, NodeEventType.Memory, nodeDepth: 0),
            CreateNode(random, NodeEventType.Rest, nodeDepth: 0),
            CreateNode(random, NodeEventType.Item, nodeDepth: 0)
        };

        return Room.Create(
            depth: 0,
            theme: "Threshold",
            nodes: nodes,
            maxNodeDepth: maxNodeDepth);
    }

    public IReadOnlyCollection<Node> GenerateNextNodes(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var room = run.CurrentRoom;

        if (room.State != RoomState.NodeResolved)
        {
            throw new Leds.GameEngine.Domain.Common.DomainException(
                "Current node event must be resolved before adding next nodes.");
        }
        var resolvedNode = room.GetResolvedNodeAtCurrentDepth();
        var nextNodeDepth = room.CurrentNodeDepth + 1;

        var random = CreateDeterministicRandom(
            $"{run.Seed}:room:{room.Depth}:next-depth:{nextNodeDepth}:parent-type:{resolvedNode.EventType}:parent-risk:{resolvedNode.RiskLevel}");

        if (nextNodeDepth == room.MaxNodeDepth)
        {
            return new[]
            {
                CreateNode(
                    random,
                    NodeEventType.RoomBoss,
                    nodeDepth: nextNodeDepth,
                    parentNodeId: resolvedNode.Id,
                    isRoomBossNode: true)
            };
        }

        var nodeCount = random.Next(1, 5);

        var eventTypes = new[]
        {
            NodeEventType.Combat,
            NodeEventType.Memory,
            NodeEventType.Rest,
            NodeEventType.Item,
            NodeEventType.Npc,
            NodeEventType.Merchant,
            NodeEventType.Law,
            NodeEventType.Curse,
            NodeEventType.Rare
        };

        return Enumerable
            .Range(0, nodeCount)
            .Select(_ =>
            {
                var eventType = eventTypes[random.Next(eventTypes.Length)];

                return CreateNode(
                    random,
                    eventType,
                    nodeDepth: nextNodeDepth,
                    parentNodeId: resolvedNode.Id,
                    isRoomBossNode: false);
            })
            .ToArray();
    }

    private static Node CreateNode(
        Random random,
        NodeEventType eventType,
        int nodeDepth,
        NodeId? parentNodeId = null,
        bool isRoomBossNode = false)
    {
        var riskLevel = eventType switch
        {
            NodeEventType.Rest => random.Next(0, 11),
            NodeEventType.Memory => random.Next(5, 26),
            NodeEventType.Item => random.Next(10, 31),
            NodeEventType.Npc => random.Next(5, 26),
            NodeEventType.Merchant => random.Next(0, 16),
            NodeEventType.Law => random.Next(20, 51),
            NodeEventType.Curse => random.Next(40, 81),
            NodeEventType.Rare => random.Next(25, 61),
            NodeEventType.Elite => random.Next(60, 91),
            NodeEventType.RoomBoss => random.Next(70, 96),
            NodeEventType.FinalBoss => random.Next(90, 101),
            NodeEventType.Combat => random.Next(20, 51),
            _ => random.Next(0, 51)
        };

        var rewardProfile = eventType switch
        {
            NodeEventType.Rest => "none",
            NodeEventType.Memory => "narrative",
            NodeEventType.Item => "common",
            NodeEventType.Npc => "narrative-choice",
            NodeEventType.Merchant => "trade",
            NodeEventType.Law => "law-modifier",
            NodeEventType.Curse => "high-risk",
            NodeEventType.Rare => "rare",
            NodeEventType.Elite => "elite",
            NodeEventType.RoomBoss => "room-boss",
            NodeEventType.FinalBoss => "final-boss",
            NodeEventType.Combat => "combat-common",
            _ => "common"
        };

        return Node.Create(
            eventType,
            riskLevel,
            rewardProfile,
            nodeDepth,
            parentNodeId,
            isRoomBossNode);
    }

    private static Random CreateDeterministicRandom(string seed)
    {
        var hash = StableHash(seed);
        return new Random(hash);
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;

            foreach (var character in value)
            {
                hash = hash * 31 + character;
            }

            return hash;
        }
    }
}