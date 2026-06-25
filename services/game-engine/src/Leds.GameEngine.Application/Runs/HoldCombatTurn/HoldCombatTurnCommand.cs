using Leds.GameEngine.Application.Combats.Actions;
using MediatR;

namespace Leds.GameEngine.Application.Runs.HoldCombatTurn;

/// <summary>
/// "Time flows" while the player holds their (ready) turn: advances the ATB clock
/// by <paramref name="DeltaTicks"/>, charging the player past the threshold and
/// filling enemies. Any enemy that becomes ready acts immediately. The player keeps
/// control (charged) afterwards.
/// </summary>
public sealed record HoldCombatTurnCommand(Guid RunId, Guid CombatId, int DeltaTicks)
    : IRequest<CombatSkillActionResult>;