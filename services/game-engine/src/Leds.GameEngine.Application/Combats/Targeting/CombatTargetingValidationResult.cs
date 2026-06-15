using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Targeting;

public sealed record CombatTargetingValidationResult(
    bool IsValid,
    string? ErrorMessage,
    IReadOnlyCollection<Combatant> Targets);