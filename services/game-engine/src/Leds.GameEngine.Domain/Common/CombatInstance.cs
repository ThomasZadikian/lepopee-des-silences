using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class CombatInstance
{
    private readonly List<CombatantSnapshot> _combatants;
    private readonly List<CombatantId> _turnOrder;
    private readonly DamageResolver _damageResolver;

    private CombatInstance(
        CombatId id,
        IReadOnlyCollection<CombatantSnapshot> combatants,
        IReadOnlyCollection<CombatantId> turnOrder,
        CombatState state,
        int currentTurnIndex,
        int round,
        DamageResolver damageResolver)
    {
        Id = id;
        _combatants = combatants.ToList();
        _turnOrder = turnOrder.ToList();
        State = state;
        CurrentTurnIndex = currentTurnIndex;
        Round = round;
        _damageResolver = damageResolver;
    }

    public CombatId Id { get; }

    public IReadOnlyCollection<CombatantSnapshot> Combatants => _combatants.AsReadOnly();

    public IReadOnlyCollection<CombatantId> TurnOrder => _turnOrder.AsReadOnly();

    public CombatState State { get; private set; }

    public int CurrentTurnIndex { get; private set; }

    public int Round { get; private set; }

    public CombatantId CurrentActorId
    {
        get
        {
            if (State != CombatState.InProgress)
            {
                throw new DomainException("Completed combat has no current actor.");
            }

            return _turnOrder[CurrentTurnIndex];
        }
    }

    public static CombatInstance Create(
        IReadOnlyCollection<CombatantSnapshot> combatants)
    {
        var combatantList = combatants?.ToList()
            ?? throw new DomainException("Combatants are required.");

        if (combatantList.Count < 2)
        {
            throw new DomainException("Combat requires at least two combatants.");
        }

        if (combatantList.Select(combatant => combatant.Id).Distinct().Count() != combatantList.Count)
        {
            throw new DomainException("Combatant ids must be unique.");
        }

        if (!combatantList.Any(combatant => combatant.Side == CombatantSide.Player))
        {
            throw new DomainException("Combat requires at least one player-side combatant.");
        }

        if (!combatantList.Any(combatant => combatant.Side == CombatantSide.Enemy))
        {
            throw new DomainException("Combat requires at least one enemy-side combatant.");
        }

        if (combatantList.Any(combatant => combatant.IsDefeated))
        {
            throw new DomainException("Combat cannot start with defeated combatants.");
        }

        var turnOrder = combatantList
            .OrderByDescending(combatant => combatant.Speed)
            .ThenBy(combatant => combatant.Id.Value)
            .Select(combatant => combatant.Id)
            .ToArray();

        return new CombatInstance(
            CombatId.New(),
            combatantList,
            turnOrder,
            CombatState.InProgress,
            currentTurnIndex: 0,
            round: 1,
            new DamageResolver());
    }

    public CombatActionResult SubmitAction(CombatAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (State != CombatState.InProgress)
        {
            throw new DomainException("Only an in-progress combat can receive actions.");
        }

        if (action.ActionType != CombatActionType.BasicAttack)
        {
            throw new DomainException($"Combat action type '{action.ActionType}' is not supported.");
        }

        if (action.ActorId != CurrentActorId)
        {
            throw new DomainException("It is not this combatant's turn.");
        }

        var actor = GetCombatant(action.ActorId);
        var target = GetCombatant(action.TargetId);

        if (actor.Side == target.Side)
        {
            throw new DomainException("Combatant cannot target an ally.");
        }

        var damageResult = _damageResolver.ResolveBasicAttack(actor, target);

        target.ReceiveDamage(damageResult.Damage);

        var winningSide = ResolveWinningSide();

        if (winningSide is not null)
        {
            State = CombatState.Completed;
        }
        else
        {
            AdvanceTurn();
        }

        return new CombatActionResult(
            Id,
            actor.Id,
            target.Id,
            action.ActionType,
            damageResult.Damage,
            target.CurrentHealth,
            target.IsDefeated,
            State,
            winningSide,
            State == CombatState.InProgress ? CurrentActorId : null,
            Round);
    }

    private CombatantSnapshot GetCombatant(CombatantId combatantId)
    {
        var combatant = _combatants.SingleOrDefault(candidate =>
            candidate.Id == combatantId);

        if (combatant is null)
        {
            throw new DomainException($"Combatant '{combatantId}' was not found.");
        }

        if (combatant.IsDefeated)
        {
            throw new DomainException("Defeated combatant cannot act or be targeted.");
        }

        return combatant;
    }

    private CombatantSide? ResolveWinningSide()
    {
        var playerSideDefeated = _combatants
            .Where(combatant => combatant.Side == CombatantSide.Player)
            .All(combatant => combatant.IsDefeated);

        if (playerSideDefeated)
        {
            return CombatantSide.Enemy;
        }

        var enemySideDefeated = _combatants
            .Where(combatant => combatant.Side == CombatantSide.Enemy)
            .All(combatant => combatant.IsDefeated);

        if (enemySideDefeated)
        {
            return CombatantSide.Player;
        }

        return null;
    }

    private void AdvanceTurn()
    {
        for (var attempt = 0; attempt < _turnOrder.Count; attempt++)
        {
            CurrentTurnIndex++;

            if (CurrentTurnIndex >= _turnOrder.Count)
            {
                CurrentTurnIndex = 0;
                Round++;
            }

            var nextCombatant = _combatants.Single(combatant =>
                combatant.Id == _turnOrder[CurrentTurnIndex]);

            if (!nextCombatant.IsDefeated)
            {
                return;
            }
        }

        throw new DomainException("Combat turn could not advance to an active combatant.");
    }
}