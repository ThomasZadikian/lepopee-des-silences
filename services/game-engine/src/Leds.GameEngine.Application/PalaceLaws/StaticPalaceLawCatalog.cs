using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.PalaceLaws;

public sealed class StaticPalaceLawCatalog : IPalaceLawCatalog
{
    public PalaceLaw GetDefaultLawFor(CurrentEventChoiceResolutionContext context)
    {
        return PalaceLaw.Create(
            key: "law-silence-v1",
            name: "Loi du Silence",
            version: "1.0.0",
            domains:
            [
                PalaceLawDomain.Generation,
                PalaceLawDomain.Combat,
                PalaceLawDomain.Rewards,
            ],
            effects:
            [
                // Combat: +10% enemy difficulty, permanent for the run.
                PalaceLawEffect.Create(
                    RunModifierType.PermanentCombatDifficultyBonus,
                    value: 0.10,
                    RunModifierDuration.UntilRunEnds),

                // Rewards: +15% reward power after each combat.
                PalaceLawEffect.Create(
                    RunModifierType.RewardPowerMultiplierBonus,
                    value: 0.15,
                    RunModifierDuration.UntilRunEnds),

                // Generation: handled in EncounterCompositionPolicy
                // via ActivePalaceLaws (budget +1 per law).
                // No RunModifier needed — the effect is structural, not stateful.
            ]);
    }
}