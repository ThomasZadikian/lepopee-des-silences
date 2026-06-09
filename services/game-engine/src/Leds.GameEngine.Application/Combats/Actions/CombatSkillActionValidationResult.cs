using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Actions;

public sealed record CombatSkillActionValidationResult(
    bool IsValid,
    string? ErrorMessage,
    Combatant? Actor,
    CombatantSkill? Skill,
    IReadOnlyCollection<Combatant> Targets);
