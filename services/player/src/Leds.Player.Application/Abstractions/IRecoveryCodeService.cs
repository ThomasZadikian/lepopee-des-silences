namespace Leds.Player.Application.Abstractions;

public sealed record RecoveryCodeBatch(
    IReadOnlyCollection<string> RawCodes,
    IReadOnlyCollection<string> Hashes);

public interface IRecoveryCodeService
{
    RecoveryCodeBatch Generate(int count = 10);
    string Hash(string rawCode);
}
