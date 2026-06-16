using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Effects;

public interface IRuntimeEffectResolver
{
    RuntimeEffectResolution Resolve(RuntimeEffectResolutionContext context);
}

public sealed record RuntimeEffectResolutionContext(
    RuntimeEffect Effect,
    IReadOnlyCollection<Combatant> Targets,
    int? MaxVitality = null,
    string SourceType = "System",
    string SourceKey = "unknown");

public sealed record RuntimeEffectResolution(
    IReadOnlyCollection<Combatant> UpdatedTargets,
    IReadOnlyCollection<RunModifier> CreatedModifiers);
