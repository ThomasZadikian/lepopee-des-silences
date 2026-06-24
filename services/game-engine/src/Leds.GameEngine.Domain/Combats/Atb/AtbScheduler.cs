namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Pure, deterministic ATB clock. Given every combatant's gauge state and a
/// current tick, it jumps straight to the next moment a combatant is ready to act
/// (event-jump — no per-tick loop) and returns the updated gauges plus the actor.
/// <para>
/// Integer math only, no RNG and no wall-clock: identical inputs always yield the
/// identical schedule. The "feel" (continuous gauges, pseudo real-time) is
/// reconstructed on the client from the returned elapsed ticks; the truth here is
/// discrete and reproducible.
/// </para>
/// <para>
/// Charge (holding past the threshold) and interruption (pushing a gauge back) are
/// expressed as gauge mutations applied between <see cref="Advance"/> calls by the
/// combat layer; the scheduler only honours the resulting gauges.
/// </para>
/// </summary>
public static class AtbScheduler
{
    public static AtbAdvanceResult Advance(IReadOnlyList<AtbParticipant> participants, long currentTick)
    {
        ArgumentNullException.ThrowIfNull(participants);

        long? minTicks = null;
        foreach (var participant in participants)
        {
            if (!participant.IsActive)
            {
                continue;
            }

            var ticks = TicksToReady(participant, currentTick);
            if (ticks is null)
            {
                continue;
            }

            if (minTicks is null || ticks < minTicks)
            {
                minTicks = ticks;
            }
        }

        if (minTicks is null)
        {
            return new AtbAdvanceResult(null, 0, currentTick, participants);
        }

        var delta = Math.Min(minTicks.Value, AtbConstants.MaxTicksPerAdvance);
        var newTick = currentTick + delta;

        var updated = new List<AtbParticipant>(participants.Count);
        foreach (var participant in participants)
        {
            updated.Add(participant.IsActive ? Fill(participant, currentTick, delta) : participant);
        }

        AtbParticipant? winner = null;
        foreach (var participant in updated)
        {
            if (!participant.IsActive || !participant.IsReady)
            {
                continue;
            }

            if (winner is null || HasPriority(participant, winner))
            {
                winner = participant;
            }
        }

        return new AtbAdvanceResult(winner?.CombatantId, delta, newTick, updated);
    }

    private static long? TicksToReady(AtbParticipant participant, long currentTick)
    {
        if (participant.IsReady)
        {
            return 0;
        }

        if (participant.FillPerTick <= 0)
        {
            return null;
        }

        var recoveryWait = Math.Max(0, participant.RecoveryUntilTick - currentTick);
        var remaining = AtbConstants.ReadyThreshold - participant.Gauge;
        var fillTicks = (remaining + participant.FillPerTick - 1) / participant.FillPerTick; // ceil
        return recoveryWait + fillTicks;
    }

    private static AtbParticipant Fill(AtbParticipant participant, long currentTick, long delta)
    {
        var recoveryWait = Math.Max(0, participant.RecoveryUntilTick - currentTick);
        var fillTicks = Math.Max(0, delta - recoveryWait);
        if (fillTicks == 0)
        {
            return participant;
        }

        var raw = participant.Gauge + (long)participant.FillPerTick * fillTicks;
        var cap = (long)AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow;
        var clamped = (int)Math.Min(raw, cap);
        return participant with { Gauge = clamped };
    }

    private static bool HasPriority(AtbParticipant candidate, AtbParticipant current)
    {
        if (candidate.Gauge != current.Gauge)
        {
            return candidate.Gauge > current.Gauge;
        }

        if (candidate.Initiative != current.Initiative)
        {
            return candidate.Initiative > current.Initiative;
        }

        return candidate.CombatantId.CompareTo(current.CombatantId) < 0;
    }
}