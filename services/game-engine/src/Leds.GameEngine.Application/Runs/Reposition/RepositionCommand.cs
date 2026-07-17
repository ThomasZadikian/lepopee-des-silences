using Leds.GameEngine.Application.Combats.Actions;
using MediatR;

namespace Leds.GameEngine.Application.Runs.Reposition;

/// <summary>
/// Changes the actor's row (Front/Back) mid-combat. Costs the actor's whole turn,
/// like a basic attack — see Combatant.SetRow and the row positioning ruleset.
/// </summary>
public sealed record RepositionCommand(
    Guid RunId,
    Guid CombatId,
    Guid ActorId,
    string Row)
    : IRequest<CombatSkillActionResult>;
