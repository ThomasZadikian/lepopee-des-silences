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

        return new Combat(
            id,
            runId,
            roomId,
            nodeId,
            CombatStatus.Active,
            allies.ToArray(),
            enemies.ToArray(),
            activeCombatantId: allies.First().Id,
            turnNumber: 1,
            createdAtUtc: DateTime.UtcNow);
    }

    public void MarkCompleted()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be completed.");

        Status = CombatStatus.Completed;
    }

    public void MarkFailed()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be failed.");

        Status = CombatStatus.Failed;
    }
}
