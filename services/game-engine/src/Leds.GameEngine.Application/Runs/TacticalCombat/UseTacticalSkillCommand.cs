using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Fait agir le combattant actif sur une case. La zone d'effet est dérivée du mode de ciblage
/// de la compétence, puis centrée sur la case visée — c'est ce qui donne son sens au
/// positionnement, là où l'ATB frappe « tous les ennemis » sans notion de distance.
/// </summary>
public sealed record UseTacticalSkillCommand(
    Guid RunId, string SkillKey, int TargetX, int TargetY)
    : IRequest<TacticalCombatResponse>;
