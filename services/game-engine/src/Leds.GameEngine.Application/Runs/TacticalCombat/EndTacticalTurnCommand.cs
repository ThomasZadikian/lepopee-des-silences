using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Passe la main au combattant suivant, puis joue d'affilée tous les tours ennemis jusqu'à ce
/// qu'un allié reprenne la main (ou que le combat s'achève).
/// </summary>
public sealed record EndTacticalTurnCommand(Guid RunId) : IRequest<TacticalCombatResponse>;
