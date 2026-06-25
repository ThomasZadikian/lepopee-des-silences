using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Domain.Combats;

public sealed class Combat
{
    private Combat(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        CombatStatus status,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        CombatantId? activeCombatantId,
        int turnNumber,
        int currentTick,
        DateTime createdAtUtc)
    {
        Id = id;
        RunId = runId;
        RoomId = roomId;
        NodeId = nodeId;
        Status = status;
        Allies = allies;
        Enemies = enemies;
        ActiveCombatantId = activeCombatantId;
        TurnNumber = turnNumber;
        CurrentTick = currentTick;
        CreatedAtUtc = createdAtUtc;
    }

    public CombatId Id { get; }
    public RunId RunId { get; }
    public RoomId RoomId { get; }
    public NodeId NodeId { get; }
    public CombatStatus Status { get; private set; }
    public IReadOnlyCollection<Combatant> Allies { get; }
    public IReadOnlyCollection<Combatant> Enemies { get; }
    public CombatantId? ActiveCombatantId { get; private set; }
    public int TurnNumber { get; private set; }

    /// <summary>The ATB clock for this combat (advances in deterministic ticks).</summary>
    public int CurrentTick { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public bool HasLivingAllies => Allies.Any(a => !a.IsDefeated);

    public bool HasLivingEnemies => Enemies.Any(e => !e.IsDefeated);

    private IEnumerable<Combatant> AllCombatants => Allies.Concat(Enemies);

    public static Combat Create(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Combat id is required.");

        if (!allies.Any())
            throw new DomainException("Combat requires at least one ally.");

        if (!enemies.Any())
            throw new DomainException("Combat requires at least one enemy.");

        if (allies.Any(a => a.IsDefeated))
            throw new DomainException("Combat cannot start with a defeated ally.");

        if (enemies.Any(e => e.IsDefeated))
            throw new DomainException("Combat cannot start with a defeated enemy.");

        // A valid opening actor (initiative order). The ATB preparer re-runs the
        // scheduler once fill rates/opening gauges are baked to set the real opener.
        var openingOrder = OrderByInitiative(allies.Concat(enemies));

        return new Combat(
            id,
            runId,
            roomId,
            nodeId,
            CombatStatus.Active,
            allies.ToArray(),
            enemies.ToArray(),
            activeCombatantId: openingOrder[0].Id,
            turnNumber: 1,
            currentTick: 0,
            createdAtUtc: DateTime.UtcNow);
    }

    public void MarkCompleted()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be completed.");

        Status = CombatStatus.Completed;
        ActiveCombatantId = null;
    }

    public void MarkFailed()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be failed.");

        Status = CombatStatus.Failed;
        ActiveCombatantId = null;
    }

    public Combatant? GetActiveCombatant()
    {
        if (Status != CombatStatus.Active || ActiveCombatantId is null)
            return null;

        return AllCombatants.FirstOrDefault(c => c.Id == ActiveCombatantId && !c.IsDefeated);
    }

    public void EnsureActorCanAct(Guid actorId)
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Combat is not active.");

        var actor = AllCombatants.FirstOrDefault(c => c.Id.Value == actorId)
            ?? throw new DomainException("Actor does not exist in this combat.");

        if (actor.IsDefeated)
            throw new DomainException("Defeated combatants cannot act.");

        if (ActiveCombatantId is null || actor.Id != ActiveCombatantId)
            throw new DomainException("It is not this combatant's turn.");
    }

    public void CompleteIfAllEnemiesDefeated()
    {
        if (Status == CombatStatus.Active && !HasLivingEnemies)
        {
            Status = CombatStatus.Completed;
            ActiveCombatantId = null;
        }
    }

    public void FailIfAllAlliesDefeated()
    {
        if (Status == CombatStatus.Active && !HasLivingAllies)
        {
            Status = CombatStatus.Failed;
            ActiveCombatantId = null;
        }
    }

    /// <summary>
    /// Advances the ATB clock to the next ready combatant and makes it active.
    /// Call <see cref="RegisterActionTaken"/> first (after an action resolves) so the
    /// actor's gauge is consumed and its recovery is set before the clock advances.
    /// </summary>
    public void AdvanceTurn()
    {
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();
        if (Status != CombatStatus.Active)
            return;

        // The combatant that just had its turn spends its gauge before the clock
        // advances, so the scheduler elects the NEXT ready combatant rather than
        // re-selecting the one that just acted.
        var outgoing = AllCombatants.FirstOrDefault(c => c.Id == ActiveCombatantId);
        if (outgoing is not null && !outgoing.IsDefeated && outgoing.AtbGauge >= AtbConstants.ReadyThreshold)
            outgoing.SetAtbGauge(0);

        var combatants = AllCombatants.ToArray();

        var participants = combatants
            .Select(c => new AtbParticipant(
                CombatantId: c.Id.Value,
                Gauge: c.AtbGauge,
                FillPerTick: c.AtbFillPerTick,
                RecoveryUntilTick: c.AtbRecoveryUntilTick,
                Initiative: c.BaseStatSnapshot.Initiative,
                IsActive: !c.IsDefeated))
            .ToArray();

        var result = AtbScheduler.Advance(participants, CurrentTick);

        foreach (var participant in result.Participants)
        {
            var combatant = combatants.First(c => c.Id.Value == participant.CombatantId);
            if (!combatant.IsDefeated)
                combatant.SetAtbGauge(participant.Gauge);
        }

        CurrentTick = (int)Math.Min(result.CurrentTick, int.MaxValue);

        if (result.NextActorId is null)
        {
            Status = CombatStatus.Failed;
            ActiveCombatantId = null;
            return;
        }

        ActiveCombatantId = new CombatantId(result.NextActorId.Value);
        TurnNumber++;

        GetActiveCombatant()?.ResetGuardToBase();
    }

    /// <summary>
    /// Records that the active actor just took an action: its gauge is consumed and
    /// it enters recovery for <paramref name="recoveryTicks"/> ticks.
    /// </summary>
    public void RegisterActionTaken(Guid actorId, int recoveryTicks)
    {
        AllCombatants.FirstOrDefault(c => c.Id.Value == actorId)
            ?.RegisterAtbAction(CurrentTick, recoveryTicks);
    }

    /// <summary>
    /// Interruption / stagger: pushes the target's ATB gauge back (larger push and
    /// charge loss if the target was charging).
    /// </summary>
    public void ApplyAtbInterruption(Guid targetId)
    {
        var target = AllCombatants.FirstOrDefault(c => c.Id.Value == targetId);
        if (target is null || target.IsDefeated)
            return;

        target.SetAtbGauge(AtbActionMath.Interrupt(target.AtbGauge));
    }

    /// <summary>
    /// "Time flows" while the player holds: advances the clock by a fixed delta,
    /// filling every combatant (the player charges past the threshold up to the
    /// overflow cap; enemies fill toward readiness). Does not change the active
    /// combatant. Returns the ids of enemies that have become ready to act,
    /// readiest first — the caller resolves them while the player keeps holding.
    /// </summary>
    public IReadOnlyCollection<Guid> HoldTick(int deltaTicks)
    {
        if (Status != CombatStatus.Active || deltaTicks <= 0)
            return [];

        var cap = AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow;

        foreach (var combatant in AllCombatants.Where(c => !c.IsDefeated))
        {
            var recoveryWait = Math.Max(0, combatant.AtbRecoveryUntilTick - CurrentTick);
            var fillTicks = Math.Max(0, deltaTicks - recoveryWait);
            if (fillTicks <= 0)
                continue;

            var raw = (long)combatant.AtbGauge + (long)combatant.AtbFillPerTick * fillTicks;
            combatant.SetAtbGauge((int)Math.Min(raw, cap));
        }

        CurrentTick += deltaTicks;

        return Enemies
            .Where(e => !e.IsDefeated && e.AtbGauge >= AtbConstants.ReadyThreshold)
            .OrderByDescending(e => e.AtbGauge)
            .ThenByDescending(e => e.BaseStatSnapshot.Initiative)
            .ThenBy(e => e.Id.Value)
            .Select(e => e.Id.Value)
            .ToArray();
    }

    /// <summary>
    /// Forces a living combatant to be the active one. Used by the hold loop to
    /// resolve a specific ready enemy's turn while the player is holding.
    /// </summary>
    public void MakeActiveCombatant(Guid combatantId)
    {
        var target = AllCombatants.FirstOrDefault(c => c.Id.Value == combatantId && !c.IsDefeated);
        if (target is not null)
            ActiveCombatantId = target.Id;
    }

    public Combatant? GetNextActiveCombatant()
    {
        if (Status != CombatStatus.Active)
            return null;

        var combatants = AllCombatants.ToArray();
        var participants = combatants
            .Select(c => new AtbParticipant(
                c.Id.Value, c.AtbGauge, c.AtbFillPerTick, c.AtbRecoveryUntilTick,
                c.BaseStatSnapshot.Initiative, !c.IsDefeated))
            .ToArray();

        var result = AtbScheduler.Advance(participants, CurrentTick);
        if (result.NextActorId is null)
            return null;

        return combatants.FirstOrDefault(c => c.Id.Value == result.NextActorId.Value);
    }

    /// <summary>
    /// Rehydrates a combat from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay combat.
    /// </summary>
    public static Combat Rehydrate(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        CombatStatus status,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        CombatantId? activeCombatantId,
        int turnNumber,
        DateTime createdAtUtc,
        int currentTick = 0)
    {
        return new Combat(id, runId, roomId, nodeId, status, allies, enemies, activeCombatantId, turnNumber, currentTick, createdAtUtc);
    }

    private static Combatant[] OrderByInitiative(IEnumerable<Combatant> combatants)
    {
        return combatants
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .ToArray();
    }
}