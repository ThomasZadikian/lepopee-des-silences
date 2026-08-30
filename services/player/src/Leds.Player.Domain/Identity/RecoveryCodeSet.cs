using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public sealed class RecoveryCodeSet
{
    private readonly HashSet<string> _remainingHashes;

    private RecoveryCodeSet(IEnumerable<string> recoveryCodeHashes)
    {
        _remainingHashes = new HashSet<string>(recoveryCodeHashes, StringComparer.Ordinal);
    }

    public int RemainingCount => _remainingHashes.Count;

    public static RecoveryCodeSet Create(IReadOnlyCollection<string> recoveryCodeHashes)
    {
        ArgumentNullException.ThrowIfNull(recoveryCodeHashes);

        if (recoveryCodeHashes.Count == 0 || recoveryCodeHashes.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("At least one valid recovery-code hash is required.");

        return new RecoveryCodeSet(recoveryCodeHashes);
    }

    public bool TryConsume(string recoveryCodeHash)
    {
        if (string.IsNullOrWhiteSpace(recoveryCodeHash))
            return false;

        return _remainingHashes.Remove(recoveryCodeHash);
    }
}
