namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Technical recovery metadata. It must never be used as a player-facing save status.
/// </summary>
public enum TechnicalRecoveryState
{
    None = 0,
    RecoveryAvailable = 1
}
