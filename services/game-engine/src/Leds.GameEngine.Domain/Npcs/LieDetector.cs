using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// Pure assessment of a player claim — SFD Système global de dialogues §5.3. Deliberately
/// decoupled from <see cref="Knowledge.KnowledgeEntry"/>: the caller resolves whether the claim
/// contradicts a confirmed fact and passes the answer in, so this stays a Domain.Npcs-only
/// concern with no dependency on the Knowledge namespace.
/// </summary>
public static class LieDetector
{
    /// <param name="suspicionScore">The NPC's current <see cref="RelationshipAxis.Suspicion"/> score.</param>
    /// <param name="doubtThreshold">Authored per-NPC personality knob — at or above this, the
    /// claim is no longer taken at face value.</param>
    /// <param name="detectionThreshold">Authored per-NPC personality knob — at or above this, the
    /// claim is recognized as false outright.</param>
    /// <param name="contradictsConfirmedFact">True when the claim disagrees with something this
    /// run already knows for certain — always <see cref="LieAssessment.Detected"/>, regardless of
    /// suspicion, since a settled fact isn't a matter of the NPC's personality to doubt.</param>
    public static LieAssessment Assess(
        int suspicionScore, int doubtThreshold, int detectionThreshold, bool contradictsConfirmedFact)
    {
        if (doubtThreshold < 0 || detectionThreshold < 0)
        {
            throw new DomainException("Lie detection thresholds must be non-negative.");
        }

        if (doubtThreshold > detectionThreshold)
        {
            throw new DomainException("The doubt threshold cannot exceed the detection threshold.");
        }

        if (contradictsConfirmedFact || suspicionScore >= detectionThreshold)
        {
            return LieAssessment.Detected;
        }

        return suspicionScore >= doubtThreshold ? LieAssessment.Doubted : LieAssessment.Believed;
    }
}
