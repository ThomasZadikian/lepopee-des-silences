namespace Leds.GameEngine.Application.Interlude.Dtos;

/// <summary>
/// Describes a top-level action available from the interlude screen
/// (e.g., "Enter Next Room", future save/abandon actions).
/// </summary>
public sealed record InterludeActionDto(
    string Key,
    string Label,
    string Description,
    bool RequiresConfirmation,
    bool IsDangerous,
    bool IsEnabled);
