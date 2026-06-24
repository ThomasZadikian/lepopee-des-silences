using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Deterministic per-tick tempo jitter for an erratic ATB feel (e.g. a
/// "Dissociated" Palais where time flows unevenly). Returns a per-mille
/// multiplier (1000 = ×1.0) derived from (seed, tick, key) via SHA-256 — never
/// <see cref="System.Random"/>, so the irregular tempo is still fully reproducible.
/// </summary>
public static class DeterministicAtbJitter
{
    public static int FactorPerMille(string seed, long tick, string key, int minPerMille, int maxPerMille)
    {
        if (maxPerMille <= minPerMille)
        {
            return minPerMille;
        }

        var input = string.Join('|', seed ?? string.Empty, tick.ToString(), key ?? string.Empty);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));
        var range = (ulong)(maxPerMille - minPerMille + 1);
        return minPerMille + (int)(value % range);
    }
}