using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

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

        var random = CreateDeterministicRandom(seed);

        var nodes = new[]
        {
            CreateNode(random, NodeEventType.Combat),
            CreateNode(random, NodeEventType.Memory),
            CreateNode(random, NodeEventType.Rest),
            CreateNode(random, NodeEventType.Item)
        };

        return Room.Create(
            depth: 0,
            theme: "Threshold",
            nodes: nodes);
    }

    private static Node CreateNode(Random random, NodeEventType eventType)
    {
        var riskLevel = eventType switch
        {
            NodeEventType.Rest => random.Next(0, 11),
            NodeEventType.Memory => random.Next(5, 26),
            NodeEventType.Item => random.Next(10, 31),
            NodeEventType.Combat => random.Next(20, 51),
            _ => random.Next(0, 51)
        };

        var rewardProfile = eventType switch
        {
            NodeEventType.Rest => "none",
            NodeEventType.Memory => "narrative",
            NodeEventType.Item => "common",
            NodeEventType.Combat => "combat-common",
            _ => "common"
        };

        return Node.Create(eventType, riskLevel, rewardProfile);
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