using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerRunStatistic
{
    private PlayerRunStatistic(Guid runId, string seed, int finalDepth, string outcome, string generatorVersion)
    {
        RunId = runId;
        Seed = seed;
        FinalDepth = finalDepth;
        Outcome = outcome;
        GeneratorVersion = generatorVersion;
    }

    public Guid RunId { get; }
    public string Seed { get; }
    public int FinalDepth { get; }
    public string Outcome { get; }
    public string GeneratorVersion { get; }

    public static PlayerRunStatistic Create(Guid runId, string seed, int finalDepth, string outcome, string generatorVersion)
    {
        if (runId == Guid.Empty) throw new DomainException("Run id is required.");
        if (string.IsNullOrWhiteSpace(seed)) throw new DomainException("Run seed is required.");
        if (finalDepth < 0) throw new DomainException("Final depth cannot be negative.");
        if (string.IsNullOrWhiteSpace(outcome)) throw new DomainException("Run outcome is required.");
        if (string.IsNullOrWhiteSpace(generatorVersion)) throw new DomainException("Generator version is required.");

        return new PlayerRunStatistic(runId, seed.Trim(), finalDepth, outcome.Trim(), generatorVersion.Trim());
    }
}
