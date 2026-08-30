using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Deterministic pseudo-random unit interval [0, 1) derived from a seed string.
/// Uses SHA-256 — never <see cref="System.Random"/> and never
/// <c>string.GetHashCode()</c> (which is randomized per process). This mirrors
/// the sampler used by the Markov/selection systems so combat critical rolls are
/// reproducible across processes, machines and .NET versions
/// (Markov SPEC §3 forbids new Random / §6 requires reproducibility).
/// </summary>
public static class DeterministicCombatRoll
{
    /// <summary>Returns a stable value in [0, 1) for the given seed.</summary>
    public static double UnitInterval(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed ?? string.Empty));
        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));
        return value / ((double)ulong.MaxValue + 1d);
    }
}