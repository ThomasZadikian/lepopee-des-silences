using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

/// <summary>
/// Lance une run. <paramref name="CombatMode"/> fixe le système de combat pour toute sa durée
/// (cf. SFD v2, §3) ; il n'est plus modifiable ensuite. Optionnel : une requête qui ne le précise
/// pas obtient l'ATB, le système historique.
/// </summary>
public sealed record StartRunCommand(
    Guid PlayerId,
    RunCombatMode CombatMode = RunCombatMode.Atb) : IRequest<StartRunResponse>;
