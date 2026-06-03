using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Produces deterministic pseudo-random values in [0, 1)
/// from stable textual inputs.
/// </summary>
public sealed class DeterministicMarkovSampler
{
    public decimal NextUnitInterval(
        string seed,
        string scope,
        string matrixKey,
        string matrixVersion,
        string currentState,
        int step)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed is required.", nameof(seed));
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope is required.", nameof(scope));
        }

        if (string.IsNullOrWhiteSpace(matrixKey))
        {
            throw new ArgumentException("Matrix key is required.", nameof(matrixKey));
        }

        if (string.IsNullOrWhiteSpace(matrixVersion))
        {
            throw new ArgumentException("Matrix version is required.", nameof(matrixVersion));
        }

        if (string.IsNullOrWhiteSpace(currentState))
        {
            throw new ArgumentException("Current state is required.", nameof(currentState));
        }

        if (step < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be non-negative.");
        }

        var input = string.Join(
            '|',
            seed.Trim(),
            scope.Trim(),
            matrixKey.Trim(),
            matrixVersion.Trim(),
            currentState.Trim(),
            step.ToString());

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        var value = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));

        return (decimal)(value / ((double)ulong.MaxValue + 1d));
    }
}