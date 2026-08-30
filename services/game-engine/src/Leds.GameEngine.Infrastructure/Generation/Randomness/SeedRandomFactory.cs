namespace Leds.GameEngine.Infrastructure.Generation.Randomness;

public sealed class SeededRandomFactory : ISeededRandomFactory
{
    public Random CreateForRoom(string seed, int roomDepth, string generatorVersion)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed is required.", nameof(seed));
        }

        if (string.IsNullOrWhiteSpace(generatorVersion))
        {
            throw new ArgumentException("Generator version is required.", nameof(generatorVersion));
        }

        var deterministicSeed = $"{seed}:room:{roomDepth}:generator:{generatorVersion}";
        return new Random(StableHash(deterministicSeed));
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