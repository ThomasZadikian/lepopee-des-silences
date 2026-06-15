using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Effects;

public sealed record CombatSkillEffectResolution(
    bool Applied,
    IReadOnlyCollection<CombatLogEntryDto> LogEntries,
    Combat Combat);