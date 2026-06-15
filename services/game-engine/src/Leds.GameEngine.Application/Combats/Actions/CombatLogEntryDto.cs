namespace Leds.GameEngine.Application.Combats.Actions;

public sealed record CombatLogEntryDto(
    DateTime OccurredAtUtc,
    string Type,
    string Message,
    Guid? ActorId,
    string? SkillKey,
    IReadOnlyCollection<Guid> TargetIds);