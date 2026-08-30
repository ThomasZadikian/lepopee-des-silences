using Leds.GameEngine.Application.Combats.Actions;

namespace Leds.GameEngine.Application.Combats.Effects;

/// <summary>
/// Ce que la résolution d'une compétence produit : le journal de ce qui s'est passé.
/// </summary>
/// <remarks>
/// Le combat lui-même n'est plus renvoyé. Il était rendu tel quel à l'appelant, qui l'avait déjà
/// passé en entrée — la même instance faisait l'aller-retour. Cette redondance forçait le contrat
/// à nommer un agrégat concret, ce qui aurait empêché le noyau de résolution de servir aussi le
/// moteur tactique (cf. SFD v2, §2). Les combattants sont mutés en place : l'appelant lit l'état
/// d'après sur sa propre référence.
/// </remarks>
public sealed record CombatSkillEffectResolution(
    IReadOnlyCollection<CombatLogEntryDto> LogEntries);
