using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Leds.GameEngine.Domain.Selection;

/// <summary>
/// Réel pseudo-aléatoire déterministe dans [0, 1) (SHA-256). Aucun System.Random,
/// aucun string.GetHashCode (SPEC §3 interdit new Random / §6 reproductibilité) —
/// stable entre processus, machines et versions de .NET, pour que toute run reste
/// rejouable depuis sa seed. Primitive RNG partagée par tout le domaine (sélection
/// pondérée, tirage de butin, etc.).
/// </summary>
public static class DeterministicSampler
{
    public static decimal NextUnitInterval(string seed, Guid runId, Guid contextId, int step)
    {
        var input = string.Join(
            '|',
            (seed ?? string.Empty).Trim(),
            runId.ToString("N"),
            contextId.ToString("N"),
            step.ToString());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));

        return (decimal)(value / ((double)ulong.MaxValue + 1d));
    }
}
