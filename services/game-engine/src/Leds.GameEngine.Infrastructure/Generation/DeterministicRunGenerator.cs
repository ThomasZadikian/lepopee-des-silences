using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    public string GeneratorVersion => "gen-0.2.0";

    public string MarkovMatrixVersion => "markov-0.2.0";

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

        var roomType = RoomType.Threshold;
        var bossProfile = ResolveBossProfile(roomType);
        var totalNodeCount = random.Next(6, 11);

        var nodes = GenerateRoomPlan(
            random,
            roomType,
            totalNodeCount);

        return Room.Create(
            depth: 0,
            roomType,
            theme: "Threshold",
            bossProfile,
            nodes);
    }

    private sealed class RoomEventGenerationState
    {
        private readonly Dictionary<NodeEventType, int> _counts = [];

        public int GetCount(NodeEventType eventType)
        {
            return _counts.GetValueOrDefault(eventType);
        }

        public void Register(IReadOnlyCollection<NodeEvent> events)
        {
            foreach (var nodeEvent in events)
            {
                _counts[nodeEvent.EventType] = GetCount(nodeEvent.EventType) + 1;
            }
        }
    }

    private static int ResolveNormalLayerCount(Random random, int normalNodeCount)
    {
        var minLayerCount = (int)Math.Ceiling(normalNodeCount / 4d);
        var maxLayerCount = Math.Min(4, normalNodeCount);

        return random.Next(minLayerCount, maxLayerCount + 1);
    }

    private static IReadOnlyCollection<Node> GenerateRoomPlan(
    Random random,
    RoomType roomType,
    int totalNodeCount)
    {
        var bossNodeCount = 1;
        var normalNodeCount = totalNodeCount - bossNodeCount;

        var normalLayerCount = ResolveNormalLayerCount(random, normalNodeCount);

        var nodes = new List<Node>();
        var eventGenerationState = new RoomEventGenerationState();

        var remainingNormalNodes = normalNodeCount;

        IReadOnlyCollection<Node> previousLayer = Array.Empty<Node>();

        for (var nodeDepth = 0; nodeDepth < normalLayerCount; nodeDepth++)
        {
            var remainingLayersAfterThisOne = normalLayerCount - nodeDepth - 1;

            var minimumNodeCountForLayer = nodeDepth == 0
                ? 2
                : 1;

            var minForThisLayer = Math.Max(
                minimumNodeCountForLayer,
                remainingNormalNodes - remainingLayersAfterThisOne * 4);

            var maxForThisLayer = Math.Min(
                4,
                remainingNormalNodes - remainingLayersAfterThisOne);

            if (minForThisLayer > maxForThisLayer)
            {
                throw new InvalidOperationException("Room plan generation failed to compute a valid node count for the current layer.");
            }

            var nodeCount = random.Next(minForThisLayer, maxForThisLayer + 1);

            var layerNodes = CreateLayerNodes(
                random,
                roomType,
                totalNodeCount,
                eventGenerationState,
                nodeDepth,
                nodeCount,
                previousLayer,
                nodeDepth == 0 ? NodeState.Available : NodeState.Planned);

            nodes.AddRange(layerNodes);

            previousLayer = layerNodes;
            remainingNormalNodes -= nodeCount;
        }

        if (remainingNormalNodes != 0)
        {
            throw new InvalidOperationException("Room plan generation failed to allocate all normal nodes.");
        }

        if (previousLayer.Count == 0)
        {
            throw new InvalidOperationException("Room plan generation failed to create a boss parent layer.");
        }

        var bossDepth = normalLayerCount;

        nodes.Add(CreateBossNode(
            roomType,
            bossDepth,
            previousLayer.Select(node => node.Id).ToArray()));

        return nodes;
    }

    private static IReadOnlyCollection<Node> CreateLayerNodes(
    Random random,
    RoomType roomType,
    int totalNodeCount,
    RoomEventGenerationState eventGenerationState,
    int nodeDepth,
    int count,
    IReadOnlyCollection<Node> parentCandidates,
    NodeState state)
    {
        if (nodeDepth == 0)
        {
            return Enumerable
                .Range(0, count)
                .Select(_ =>
                {
                    var events = CreateNodeEvents(
                        random,
                        roomType,
                        totalNodeCount,
                        eventGenerationState,
                        preferRest: false);

                    eventGenerationState.Register(events);

                    var riskLevel = ResolveRiskLevel(random, events);
                    var rewardProfile = ResolveRewardProfile(events);

                    return Node.Create(
                        events,
                        riskLevel,
                        rewardProfile,
                        nodeDepth,
                        Array.Empty<NodeId>(),
                        isRoomBossNode: false,
                        state);
                })
                .ToArray();
        }

        if (parentCandidates.Count == 0)
        {
            throw new InvalidOperationException("Non-initial node layers require parent candidates.");
        }

        var parentIdsByChildIndex = Enumerable
            .Range(0, count)
            .Select(_ => new List<NodeId>())
            .ToArray();

        var parentIndex = 0;

        foreach (var parent in parentCandidates)
        {
            parentIdsByChildIndex[parentIndex % count].Add(parent.Id);
            parentIndex++;
        }

        for (var childIndex = 0; childIndex < count; childIndex++)
        {
            if (parentIdsByChildIndex[childIndex].Count == 0)
            {
                var randomParent = parentCandidates.ElementAt(random.Next(parentCandidates.Count));
                parentIdsByChildIndex[childIndex].Add(randomParent.Id);
            }

            if (random.NextDouble() < 0.35)
            {
                var extraParent = parentCandidates.ElementAt(random.Next(parentCandidates.Count));

                if (!parentIdsByChildIndex[childIndex].Contains(extraParent.Id))
                {
                    parentIdsByChildIndex[childIndex].Add(extraParent.Id);
                }
            }
        }

        return Enumerable
            .Range(0, count)
            .Select(childIndex =>
            {
                var preferRest = nodeDepth > 0 && random.NextDouble() < 0.20;

                var events = CreateNodeEvents(
                    random,
                    roomType,
                    totalNodeCount,
                    eventGenerationState,
                    preferRest);

                eventGenerationState.Register(events);

                var riskLevel = ResolveRiskLevel(random, events);
                var rewardProfile = ResolveRewardProfile(events);

                return Node.Create(
                    events,
                    riskLevel,
                    rewardProfile,
                    nodeDepth,
                    parentIdsByChildIndex[childIndex],
                    isRoomBossNode: false,
                    state);
            })
            .ToArray();
    }

    private static Node CreateBossNode(
        RoomType roomType,
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds)
    {
        var events = new[]
        {
        NodeEvent.Create(NodeEventType.RoomBoss, 1)
    };

        return Node.Create(
            events,
            riskLevel: ResolveBossRiskLevel(roomType),
            rewardProfile: "room-boss",
            nodeDepth,
            parentNodeIds,
            isRoomBossNode: true,
            initialState: NodeState.Planned);
    }

    private static IReadOnlyCollection<NodeEvent> CreateNodeEvents(
        Random random,
        RoomType roomType,
        int totalNodeCount,
        RoomEventGenerationState eventGenerationState,
        bool preferRest)
    {
        if (preferRest &&
            eventGenerationState.GetCount(NodeEventType.Rest) == 0)
        {
            return new[]
            {
            NodeEvent.Create(NodeEventType.Rest, 1)
        };
        }

        var maxEventCount = random.Next(1, 5);
        var selectedEventTypes = new List<NodeEventType>();

        for (var order = 1; order <= maxEventCount; order++)
        {
            var candidates = ResolveAllowedEventTypes(roomType)
                .Where(eventType => CanAddEventType(
                    eventType,
                    selectedEventTypes,
                    totalNodeCount,
                    eventGenerationState))
                .ToArray();

            if (candidates.Length == 0)
            {
                break;
            }

            var selectedEventType = candidates[random.Next(candidates.Length)];

            if (selectedEventType == NodeEventType.Rest)
            {
                return new[]
                {
                NodeEvent.Create(NodeEventType.Rest, 1)
            };
            }

            selectedEventTypes.Add(selectedEventType);
        }

        if (selectedEventTypes.Count == 0)
        {
            selectedEventTypes.Add(NodeEventType.Combat);
        }

        return selectedEventTypes
            .Select((eventType, index) => NodeEvent.Create(eventType, index + 1))
            .ToArray();
    }

    private static NodeEventType[] ResolveAllowedEventTypes(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold =>
            [
                NodeEventType.Combat,
            NodeEventType.Combat,
            NodeEventType.Item,
            NodeEventType.Rest,
            NodeEventType.Npc
            ],

            RoomType.Memory =>
            [
                NodeEventType.Combat,
            NodeEventType.Item,
            NodeEventType.Rest,
            NodeEventType.Law,
            NodeEventType.Rare
            ],

            RoomType.Forest =>
            [
                NodeEventType.Combat,
            NodeEventType.Item,
            NodeEventType.Npc,
            NodeEventType.Merchant,
            NodeEventType.Rare
            ],

            RoomType.Rupture =>
            [
                NodeEventType.Combat,
            NodeEventType.Combat,
            NodeEventType.Elite,
            NodeEventType.Curse,
            NodeEventType.Law,
            NodeEventType.Rare
            ],

            RoomType.Silence =>
            [
                NodeEventType.Combat,
            NodeEventType.Rest,
            NodeEventType.Law,
            NodeEventType.Npc,
            NodeEventType.Curse
            ],

            _ =>
            [
                NodeEventType.Combat,
            NodeEventType.Combat,
            NodeEventType.Item,
            NodeEventType.Rest
            ]
        };
    }

    private static int ResolveRiskLevel(Random random, IReadOnlyCollection<NodeEvent> events)
    {
        var baseRisk = events.Sum(nodeEvent => nodeEvent.EventType switch
        {
            NodeEventType.Rest => 0,
            NodeEventType.Memory => 8,
            NodeEventType.Item => 10,
            NodeEventType.Npc => 8,
            NodeEventType.Merchant => 4,
            NodeEventType.Law => 18,
            NodeEventType.Curse => 28,
            NodeEventType.Rare => 18,
            NodeEventType.Elite => 40,
            NodeEventType.Combat => 24,
            _ => 10
        });

        var variance = random.Next(0, 16);

        return Math.Clamp(baseRisk + variance, 0, 100);
    }

    private static int ResolveBossRiskLevel(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => 70,
            RoomType.Memory => 74,
            RoomType.Forest => 76,
            RoomType.Rupture => 84,
            RoomType.Silence => 82,
            RoomType.Antechamber => 88,
            RoomType.Final => 100,
            _ => 75
        };
    }

    private static string ResolveRewardProfile(IReadOnlyCollection<NodeEvent> events)
    {
        if (events.Count == 1 && events.Single().EventType == NodeEventType.Rest)
        {
            return "healing-only";
        }

        if (events.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Rare))
        {
            return "rare";
        }

        if (events.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Elite))
        {
            return "elite";
        }

        if (events.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Curse))
        {
            return "high-risk";
        }

        if (events.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Merchant))
        {
            return "trade";
        }

        if (events.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Combat))
        {
            return "combat-common";
        }

        return "common";
    }

    private static RoomBossProfile ResolveBossProfile(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => RoomBossProfile.Create(
                "threshold-guardian",
                "Gardien du Seuil",
                roomType,
                "High"),

            RoomType.Memory => RoomBossProfile.Create(
                "fractured-archivist",
                "Archiviste Fêlé",
                roomType,
                "High"),

            RoomType.Forest => RoomBossProfile.Create(
                "root-warden",
                "Veilleur des Racines",
                roomType,
                "High"),

            RoomType.Rupture => RoomBossProfile.Create(
                "rupture-knight",
                "Chevalier de la Rupture",
                roomType,
                "VeryHigh"),

            RoomType.Silence => RoomBossProfile.Create(
                "silent-choir",
                "Chœur du Silence",
                roomType,
                "VeryHigh"),

            RoomType.Antechamber => RoomBossProfile.Create(
                "antechamber-judge",
                "Juge de l’Antichambre",
                roomType,
                "Extreme"),

            RoomType.Final => RoomBossProfile.Create(
                "him-lit",
                "Him’Lit",
                roomType,
                "Final"),

            _ => RoomBossProfile.Create(
                "unknown-boss",
                "Présence inconnue",
                roomType,
                "Unknown")
        };
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

    private static bool CanAddEventType(
    NodeEventType eventType,
    IReadOnlyCollection<NodeEventType> currentNodeEventTypes,
    int totalNodeCount,
    RoomEventGenerationState eventGenerationState)
    {
        return eventType switch
        {
            NodeEventType.Combat => true,

            NodeEventType.Elite =>
                eventGenerationState.GetCount(NodeEventType.Elite) == 0 &&
                !currentNodeEventTypes.Contains(NodeEventType.Elite),

            NodeEventType.Item =>
                !currentNodeEventTypes.Contains(NodeEventType.Item) &&
                eventGenerationState.GetCount(NodeEventType.Item) < totalNodeCount / 2,

            NodeEventType.Npc =>
                eventGenerationState.GetCount(NodeEventType.Npc) == 0 &&
                !currentNodeEventTypes.Contains(NodeEventType.Npc),

            NodeEventType.Memory => false,

            NodeEventType.Rest =>
                eventGenerationState.GetCount(NodeEventType.Rest) == 0 &&
                currentNodeEventTypes.Count == 0,

            NodeEventType.Merchant =>
                eventGenerationState.GetCount(NodeEventType.Merchant) == 0 &&
                !currentNodeEventTypes.Contains(NodeEventType.Merchant),

            NodeEventType.Law =>
                eventGenerationState.GetCount(NodeEventType.Law) == 0 &&
                !currentNodeEventTypes.Contains(NodeEventType.Law),

            NodeEventType.Curse =>
                eventGenerationState.GetCount(NodeEventType.Curse) == 0 &&
                !currentNodeEventTypes.Contains(NodeEventType.Curse),

            NodeEventType.Rare =>
                eventGenerationState.GetCount(NodeEventType.Rare) < 3 &&
                !currentNodeEventTypes.Contains(NodeEventType.Rare),

            NodeEventType.RoomBoss => false,
            NodeEventType.FinalBoss => false,

            _ => false
        };
    }
}