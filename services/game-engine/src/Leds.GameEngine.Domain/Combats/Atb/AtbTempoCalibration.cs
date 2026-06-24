using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Single tuning surface mapping the Palace's psyche (dominant emotional state)
/// to ATB tempo. All factors are per-mille (1000 = ×1.0) for integer determinism.
/// This is where Markov "drives time": the latent emotional distribution of the
/// run warps how fast gauges fill, globally and per side.
/// </summary>
public static class AtbTempoCalibration
{
    public const int JitterMinPerMille = 800;
    public const int JitterMaxPerMille = 1200;

    public static int RoomFactorPerMille(EmotionalState state) => state switch
    {
        EmotionalState.Lucid => 1100,
        EmotionalState.Wary => 1050,
        EmotionalState.Calm => 1000,
        EmotionalState.Withdrawn => 900,
        EmotionalState.Dissociated => 1000, // jittered per tick by the caller
        EmotionalState.Fragmented => 1000,  // per-combatant jitter by the caller
        _ => 1000
    };

    public static int CombatantFactorPerMille(CombatantSide side, EmotionalState state) => (side, state) switch
    {
        (CombatantSide.Enemy, EmotionalState.Wary) => 1100,
        (CombatantSide.Enemy, EmotionalState.Dissociated) => 1100,
        (CombatantSide.Enemy, EmotionalState.Fragmented) => 1050,
        (CombatantSide.Player, EmotionalState.Lucid) => 1100,
        (CombatantSide.Player, EmotionalState.Calm) => 1050,
        (CombatantSide.Player, EmotionalState.Withdrawn) => 950,
        _ => 1000
    };

    public static int OpeningBias(CombatantSide side, EmotionalState state)
    {
        if (side != CombatantSide.Enemy)
        {
            return 0;
        }

        return state switch
        {
            EmotionalState.Wary => 2_000,
            EmotionalState.Dissociated => 2_500,
            EmotionalState.Fragmented => 1_500,
            _ => 0
        };
    }
}