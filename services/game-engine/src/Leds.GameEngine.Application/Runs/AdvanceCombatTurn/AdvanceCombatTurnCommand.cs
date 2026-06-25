using Leds.GameEngine.Application.Combats.Actions;
using MediatR;

namespace Leds.GameEngine.Application.Runs.AdvanceCombatTurn;

/// <summary>
/// Advances the ATB combat by exactly one turn: if the next ready combatant is an
/// enemy it resolves that single enemy turn; if it is the player it is a no-op
/// ("your turn"). The client drives this in real time as gauges fill, instead of
/// the server cascading every enemy turn inside the player's action.
/// </summary>
public sealed record AdvanceCombatTurnCommand(Guid RunId, Guid CombatId)
    : IRequest<CombatSkillActionResult>;