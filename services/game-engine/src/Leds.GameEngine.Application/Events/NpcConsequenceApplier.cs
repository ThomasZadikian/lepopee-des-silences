using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events;

/// <summary>
/// Applies NPC reward/curse draw results to the run by reusing the existing law/curse
/// construction rules (kept isolated from the Law/Curse choice resolvers).
/// </summary>
public static class NpcConsequenceApplier
{
    public static void ApplyDamage(Run run, int amount)
    {
        if (amount > 0)
        {
            run.ApplyVitalityLoss(amount);
        }
    }

    public static void ApplyCurse(Run run, CatalogCurseDefinitionSnapshot definition)
    {
        var difficultyDelta = Math.Clamp(definition.Severity / 100.0, 0.01, 0.5);

        var curse = ActiveCurse.Create(
            definition.Key,
            definition.DisplayName,
            definition.Description,
            difficultyDelta,
            DateTime.UtcNow,
            curseDefinitionKey: definition.Key,
            severity: definition.Severity,
            duration: definition.Duration,
            effectSetKey: definition.EffectSetKey);

        run.ApplyCurse(curse);

        run.AddRunModifier(RunModifier.Create(
            RunModifierType.NextCombatDifficultyMultiplier,
            difficultyDelta,
            RunModifierDuration.NextCombatOnly,
            "Curse",
            definition.Key));
    }

    public static void ApplyLaw(Run run, PalaceLawDefinitionSnapshot definition)
    {
        run.ActivatePalaceLaw(PalaceLawMapper.CreatePalaceLaw(definition));
    }
}