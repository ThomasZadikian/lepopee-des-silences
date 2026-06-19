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
    public DateTime CreatedAtUtc { get; }
    public bool HasLivingAllies => Allies.Any(a => !a.IsDefeated);

    public bool HasLivingEnemies => Enemies.Any(e => !e.IsDefeated);

    public static Combat Create(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies)
    {
        var openingOrder = OrderByInitiative(allies.Concat(enemies)); 

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
        if (Status != CombatStatus.Active)
            return null;

        var livingCombatants = GetTurnOrder()
            .Where(c => !c.IsDefeated)
            .ToArray();

        if (livingCombatants.Length == 0)
            return null;

        var currentIndex = Array.FindIndex(livingCombatants, c => c.Id == ActiveCombatantId);

        if (currentIndex < 0)
            return livingCombatants[0];

        return livingCombatants[currentIndex];
    }

    public void EnsureActorCanAct(Guid actorId)
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Combat is not active.");

        var actor = GetTurnOrder()
            .FirstOrDefault(c => c.Id.Value == actorId)
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

    public void AdvanceTurn()
    {
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();

        if (Status != CombatStatus.Active)
            return;

        var order = GetTurnOrder();
        if (order.Length == 0)
        {
            Status = CombatStatus.Failed;
            ActiveCombatantId = null;
            return;
        }

        var currentIndex = Array.FindIndex(order, c => c.Id == ActiveCombatantId);
        if (currentIndex < 0)
            currentIndex = 0;

        for (var step = 1; step <= order.Length; step++)
        {
            var index = (currentIndex + step) % order.Length;
            if (order[index].IsDefeated)
                continue;

            var startedNewRound = currentIndex + step >= order.Length;
            if (startedNewRound)
            {
                TurnNumber++;
                foreach (var combatant in order.Where(c => !c.IsDefeated))
                    combatant.ResetGuardToBase();
            }

            ActiveCombatantId = order[index].Id;
            return;
        }

        Status = CombatStatus.Failed;
        ActiveCombatantId = null;
    }

    public Combatant? GetNextActiveCombatant()
    {
        if (Status != CombatStatus.Active)
        {
            return null;
        }

        var livingCombatants = GetTurnOrder()
            .Where(c => !c.IsDefeated)
            .ToArray();

        if (livingCombatants.Length == 0)
        {
            return null;
        }

        var currentIndex = Array.FindIndex(livingCombatants, c => c.Id == ActiveCombatantId);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % livingCombatants.Length;

        return livingCombatants[nextIndex];
    }

    private Combatant[] GetTurnOrder() => OrderByInitiative(Allies.Concat(Enemies));


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
        DateTime createdAtUtc)
    {
        return new Combat(id, runId, roomId, nodeId, status, allies, enemies, activeCombatantId, turnNumber, createdAtUtc);
    }
    private static Combatant[] OrderByInitiative(IEnumerable<Combatant> combatants)
    {
        // Déterministe et STABLE (les stats ne changent pas en combat) :
        // vitesse desc, puis initiative desc, puis side, puis id.
        return combatants
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .ToArray();
    }
}