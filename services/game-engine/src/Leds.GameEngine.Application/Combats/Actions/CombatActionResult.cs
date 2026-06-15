using Leds.GameEngine.Application.Combats.Dtos;

namespace Leds.GameEngine.Application.Combats.Actions;

public sealed record CombatActionResult(
    Guid CombatId,
    Guid ActorId,
    string SkillKey,
    IReadOnlyCollection<Guid> TargetIds,
    bool Accepted,
    string? Message,
    CombatRuntimeDto Combat,
    IReadOnlyCollection<CombatLogEntryDto> LogEntries);