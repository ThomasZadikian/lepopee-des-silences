namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Emotional damage types of the Palais. These are thematic, not elemental:
/// they echo the emotional architecture of the place (dread, denial, sorrow,
/// rupture, memory, silence).
/// <para>
/// <see cref="Neutral"/> participates in no weakness relation — it always
/// resolves to ×1.0. Any combatant or skill without a declared type is therefore
/// inert with respect to the type system, which keeps the feature additive.
/// </para>
/// </summary>
public enum EmotionalType
{
    Neutral = 0,

    /// <summary>Effroi — terror, dread.</summary>
    Effroi = 1,

    /// <summary>Déni — refusal, avoidance.</summary>
    Deni = 2,

    /// <summary>Mélancolie — sorrow, withdrawal.</summary>
    Melancolie = 3,

    /// <summary>Rupture — breaking, violence.</summary>
    Rupture = 4,

    /// <summary>Mémoire — the past, remembrance.</summary>
    Memoire = 5,

    /// <summary>Silence — the void, absence.</summary>
    Silence = 6,

    /// <summary>Folie — madness, loss of self.</summary>
    Folie = 7
}