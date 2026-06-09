using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;

namespace Leds.GameEngine.Application.Combats.EnemyTurns;

public sealed record EnemyCombatTurnResolution(
    bool WasResolved,
    Guid? ActorId,
    string? SkillKey,
    IReadOnlyCollection<Guid> TargetIds,
    IReadOnlyCollection<CombatLogEntryDto> LogEntries,
    CombatRuntimeDto Combat);
