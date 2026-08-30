using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Déplace le combattant actif vers une case. Ne consomme pas son action : se déplacer et agir
/// sont indépendants (SFD v2, §8).
/// </summary>
public sealed record MoveTacticalCombatantCommand(Guid RunId, int TargetX, int TargetY)
    : IRequest<TacticalCombatResponse>;
