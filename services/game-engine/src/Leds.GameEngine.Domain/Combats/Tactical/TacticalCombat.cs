using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// L'agrégat de combat tactique : tour par tour sur grille, ordre d'initiative dérivé de la
/// Vitesse, deux actions indépendantes par tour.
/// </summary>
/// <remarks>
/// <para>
/// Frère de <see cref="Combat"/>, pas héritier (cf. SFD v2, §2). Les deux implémentent
/// <see cref="ICombatContext"/> pour que le noyau de résolution — dégâts, statuts, DoT, lois —
/// serve les deux sans rien connaître de leur ordonnancement.
/// </para>
/// <para>
/// Ce que cet agrégat ne fait pas, volontairement : pas de jauge, pas de tempo, pas de momentum,
/// pas de coût d'investissement. L'ordre d'action est fixe et lisible d'un coup d'œil ; la
/// profondeur vient du positionnement, de la portée et des scripts ennemis, pas de l'horloge.
/// </para>
/// </remarks>
public sealed class TacticalCombat : ICombatContext
{
    private readonly List<Combatant> _allies;
    private readonly List<Combatant> _enemies;
    private readonly Dictionary<Guid, GridPosition> _positions;
    private readonly Dictionary<Guid, TacticalTurnState> _turnStates = new();
    private readonly HashSet<string> _usedOnceSkillKeys = new(StringComparer.Ordinal);
    private List<Guid> _initiativeOrder = [];
    private int _activeIndex;

    private TacticalCombat(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        TacticalBattlefield battlefield,
        List<Combatant> allies,
        List<Combatant> enemies,
        Dictionary<Guid, GridPosition> positions,
        DateTime createdAtUtc)
    {
        Id = id;
        RunId = runId;
        RoomId = roomId;
        NodeId = nodeId;
        Battlefield = battlefield;
        _allies = allies;
        _enemies = enemies;
        _positions = positions;
        CreatedAtUtc = createdAtUtc;
        Status = CombatStatus.Active;
        RoundNumber = 1;

        RebuildInitiativeOrder();
    }

    public CombatId Id { get; }
    public RunId RunId { get; }
    public RoomId RoomId { get; }
    public NodeId NodeId { get; }
    public TacticalBattlefield Battlefield { get; }
    public CombatStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }

    /// <summary>Round courant, à partir de 1. L'initiative est recalculée à chaque nouveau round.</summary>
    public int RoundNumber { get; private set; }

    public IReadOnlyCollection<Combatant> Allies => _allies.AsReadOnly();
    public IReadOnlyCollection<Combatant> Enemies => _enemies.AsReadOnly();

    private IEnumerable<Combatant> AllCombatants => _allies.Concat(_enemies);

    /// <summary>
    /// L'ordre d'action du round, du plus rapide au plus lent. Exposé parce qu'il doit
    /// être affiché : sa prévisibilité est la contrepartie de l'abandon du tempo ATB.
    /// </summary>
    public IReadOnlyList<Guid> InitiativeOrder => _initiativeOrder.AsReadOnly();
    public IReadOnlySet<string> UsedOnceSkillKeys => _usedOnceSkillKeys;

    public Guid? ActiveCombatantId =>
        _activeIndex >= 0 && _activeIndex < _initiativeOrder.Count
            ? _initiativeOrder[_activeIndex]
            : null;

    public bool HasLivingAllies => _allies.Any(a => !a.IsDefeated);
    public bool HasLivingEnemies => _enemies.Any(e => !e.IsDefeated);

    //  Positions 

    /// <summary>
    /// Cases tenues par un combattant encore debout. Un combattant à terre libère la sienne :
    /// le champ de bataille se dégage à mesure que le combat avance.
    /// </summary>
    public IReadOnlySet<GridPosition> OccupiedCells() =>
        AllCombatants
            .Where(c => !c.IsDefeated)
            .Select(c => _positions[c.Id.Value])
            .ToHashSet();

    //  conomie d'action 

    /// <summary>
    /// L'état du tour d'un combattant : s'est-il déplacé, a-t-il agi. Les deux sont indépendants
    /// (SFD v2, §8) — renoncer à l'un ne pénalise ni ne bonifie l'autre, et les quatre
    /// combinaisons sont légales.
    /// </summary>
    public sealed record TacticalTurnState(bool HasMoved, bool HasActed)
    {
        public static readonly TacticalTurnState Fresh = new(false, false);
        public bool IsSpent => HasMoved && HasActed;
    }

    public TacticalTurnState TurnStateOf(Guid combatantId) =>
        _turnStates.GetValueOrDefault(combatantId, TacticalTurnState.Fresh);

    public bool HasUsedOnceSkill(string skillKey) =>
        _usedOnceSkillKeys.Contains(skillKey);

    public void MarkOnceSkillUsed(string skillKey)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(skillKey))
            throw new DomainException("Once-per-combat skill key is required.");

        if (!_usedOnceSkillKeys.Add(skillKey))
            throw new DomainException($"Skill '{skillKey}' has already been used in this combat.");
    }

    //  Déroulé 

    /// <summary>
    /// Recalcule l'ordre d'initiative : Vitesse effective décroissante, égalités tranchées par
    /// nom pour que l'ordre reste déterministe d'une exécution à l'autre. Les combattants à
    /// terre en sortent.
    /// </summary>
    public void RebuildInitiativeOrder()
    {
        _initiativeOrder = AllCombatants
            .Where(c => !c.IsDefeated)
            .OrderByDescending(c => c.EffectiveSpeed)
            .ThenBy(c => c.DisplayName, StringComparer.Ordinal)
            .ThenBy(c => c.Id.Value)
            .Select(c => c.Id.Value)
            .ToList();

        _activeIndex = 0;
    }

    /// <summary>
    /// Appelé lorsqu'un combattant est vaincu pour le retirer immédiatement de l'ordre d'initiative.
    /// Cela évite les incohérences où un combattant mort pourrait encore avoir son tour.
    /// </summary>
    /// <param name="combatantId">L'ID du combattant vaincu.</param>
    public void OnCombatantDefeated(Guid combatantId)
    {
        // Retirer le combattant de l'ordre d'initiative
        _initiativeOrder.Remove(combatantId);

        // Si c'était le combattant actif, avancer au suivant
        if (ActiveCombatantId == combatantId)
        {
            AdvanceToNextCombatant();
        }
        // Sinon, ajuster l'index si nécessaire (ex: si on a retiré un combattant avant l'actif)
        else if (_activeIndex >= _initiativeOrder.Count)
        {
            _activeIndex = Math.Max(0, _initiativeOrder.Count - 1);
        }
    }

    /// <summary>
    /// Passe la main au combattant suivant. En fin de liste, ouvre un nouveau round : l'horloge
    /// avance d'un tour plein, les statuts sont réévalués par l'appelant, et l'initiative est
    /// recalculée — un ralentissement subi en cours de round ne prend effet qu'au round suivant,
    /// ce qui garde l'ordre affiché honnête.
    ///
    /// Modifié pour filtrer les combattants vaincus (O-005).
    /// </summary>
    public void AdvanceToNextCombatant()
    {
        EnsureActive();

        // Filtrer les combattants vaincus de l'ordre d'initiative pour éviter les incohérences
        _initiativeOrder = _initiativeOrder.Where(id => !IsDefeated(id)).ToList();

        // Si plus personne, commencer un nouveau round
        if (_initiativeOrder.Count == 0)
        {
            BeginNextRound();
            return;
        }

        // Passer au suivant (en tenant compte du filtre)
        do
        {
            _activeIndex++;

            if (_activeIndex >= _initiativeOrder.Count)
            {
                BeginNextRound();
                return;
            }
        }
        while (IsDefeated(_initiativeOrder[_activeIndex]));
    }

    private void BeginNextRound()
    {
        RoundNumber++;
        CurrentTick += CombatTime.TicksPerTurn;
        _turnStates.Clear();
        RebuildInitiativeOrder();
    }

    /// <summary>Ce qu'a coûté un déplacement, et par où il est passé.</summary>
    public sealed record TacticalMoveResult(int Cost, IReadOnlyList<GridPosition> Path);

    /// <summary>
    /// Déplace le combattant actif. Échoue si la case est hors de portée, occupée, ou s'il s'est
    /// déjà déplacé ce tour. Ne consomme pas son action : se déplacer et agir sont indépendants.
    /// </summary>
    public TacticalMoveResult MoveActiveCombatant(GridPosition destination)
    {
        EnsureActive();

        var combatantId = ActiveCombatantId
            ?? throw new DomainException("No combatant is currently active.");

        var state = TurnStateOf(combatantId);
        if (state.HasMoved)
            throw new DomainException("This combatant has already moved this turn.");

        var combatant = RequireCombatant(combatantId);
        var origin = PositionOf(combatantId);

        var occupied = OccupiedCells().ToHashSet();
        occupied.Remove(origin); // on ne se bloque pas soi-même

        var reachable = TacticalMovement.ReachableCells(
            Battlefield,
            origin,
            TacticalMovement.BudgetFor(combatant.EffectiveSpeed),
            occupied);

        if (!reachable.TryGetValue(destination, out var cost))
            throw new DomainException($"{destination} is out of this combatant's movement range.");

        // Le chemin est renvoyé avec le coût : le client doit pouvoir montrer le trajet réel,
        // qui contourne murs et montées, plutôt que de faire glisser la figure en ligne droite.
        var path = TacticalMovement.PathTo(
            Battlefield,
            origin,
            destination,
            TacticalMovement.BudgetFor(combatant.EffectiveSpeed),
            occupied) ?? [destination];

        _positions[combatantId] = destination;
        _turnStates[combatantId] = state with { HasMoved = true };
        return new TacticalMoveResult(cost, path);
    }

    /// <summary>
    /// Marque l'action du combattant actif comme dépensée. Le noyau de résolution a déjà fait le
    /// travail : cet agrégat n'enregistre que la consommation.
    /// </summary>
    public void MarkActiveCombatantActed()
    {
        EnsureActive();

        var combatantId = ActiveCombatantId
            ?? throw new DomainException("No combatant is currently active.");

        var state = TurnStateOf(combatantId);
        if (state.HasActed)
            throw new DomainException("This combatant has already acted this turn.");

        _turnStates[combatantId] = state with { HasActed = true };
    }

    /// <summary>Portée effective entre deux combattants, en distance de Manhattan.</summary>
    public int DistanceBetween(Guid a, Guid b) => PositionOf(a).ManhattanDistanceTo(PositionOf(b));

    /// <summary>
    /// Un tir depuis une position dominante gagne en précision et en puissance (SFD v2, §11).
    /// Ne vaut que pour les attaques à distance : au contact, la hauteur ne donne rien.
    /// </summary>
    public bool HasHeightAdvantage(Guid attackerId, Guid targetId)
        => Battlefield.ElevationAt(PositionOf(attackerId))
           > Battlefield.ElevationAt(PositionOf(targetId));

    public bool HasLineOfSight(Guid fromId, Guid toId)
        => TacticalMovement.HasLineOfSight(Battlefield, PositionOf(fromId), PositionOf(toId));

    //  Issue 

    public void CompleteIfAllEnemiesDefeated()
    {
        if (Status == CombatStatus.Active && !HasLivingEnemies)
            Status = CombatStatus.Completed;
    }

    public void FailIfAllAlliesDefeated()
    {
        if (Status == CombatStatus.Active && !HasLivingAllies)
            Status = CombatStatus.Failed;
    }

    //  ICombatContext 

    /// <summary>
    /// Temps courant, en ticks. Le tactique partage l'unité de l'ATB pour que les durées de
    /// statut et de DoT soient authorisées une seule fois : un round avance d'un tour plein.
    /// </summary>
    public int CurrentTick { get; private set; }

    /// <inheritdoc />
    /// <remarks>Le round tient lieu de tour : les deux systèmes comptent la même chose.</remarks>
    public int TurnNumber => RoundNumber;

    public bool LowHpDamageAmplificationEnabled { get; private init; }
    public bool HealingBlocked { get; private init; }
    public bool DuelDamageAsymmetryEnabled { get; private init; }
    public int DotMagnitudeBonus { get; private init; }
    public int DotDurationExtensionTicks { get; private init; }

    // Lisibles depuis l'extérieur uniquement pour la persistance : `HitCounter` et
    // `HasFirstHitLanded` sont des compteurs de Loi en cours de combat. Les perdre à chaque
    // sauvegarde relancerait « une frappe sur N » à zéro à chaque rechargement de partie.
    public bool HitCounterDoubleDamageEnabled { get; private init; }
    public bool FirstHitCriticalEnabled { get; private init; }
    public int HitCounter { get; private set; }
    public bool HasFirstHitLanded { get; private set; }

    public bool RegisterLandedHit()
    {
        HitCounter++;
        return HitCounterDoubleDamageEnabled && HitCounter % Combat.HitCounterTrigger == 0;
    }

    public bool TryConsumeFirstHitCritical()
    {
        if (HasFirstHitLanded)
            return false;

        HasFirstHitLanded = true;
        return FirstHitCriticalEnabled;
    }

    /// <inheritdoc />
    /// <remarks>
    /// « Loi de l'Éloge Funèbre » — pas encore portée au tactique. Elle arme la restriction du
    /// <i>prochain agissant</i>, notion qui dépend de l'ordonnancement : en ATB c'est celui dont
    /// la jauge se remplit, ici ce serait le suivant dans l'initiative. Câbler la mécanique sans
    /// avoir tranché ce point produirait une règle qui diffère silencieusement d'un mode à
    /// l'autre.
    /// </remarks>
    public void RegisterCombatantDefeated()
    {
        // Volontairement sans effet — voir la remarque.
    }

    /// <inheritdoc />
    /// <remarks>« Loi de la Troisième Tasse » — même statut que ci-dessus, en attente.</remarks>
    public (int HealAmount, bool Triggered) ApplyThirdCupRollIfActive(Combatant target, int healAmount)
        => (healAmount, false);

    /// <inheritdoc />
    /// <remarks>
    /// Sans objet : l'ordre de tour est fixe. Il n'y a pas de jauge à repousser, et retirer son
    /// tour à une cible serait une mécanique bien plus violente qu'en ATB — à concevoir comme
    /// telle si on la veut, pas à hériter par accident.
    /// </remarks>
    public void InterruptAction(Guid targetId)
    {
        // Volontairement sans effet — voir la remarque.
    }

    /// <inheritdoc />
    /// <remarks>Sans objet : le momentum accélère une reprise de main, notion propre au tempo ATB.</remarks>
    public void AwardTempoMomentum(Combatant combatant, int amountPerMille)
    {
        // Volontairement sans effet — voir la remarque.
    }

    //  Fabrique 

    public static TacticalCombat Create(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        TacticalBattlefield battlefield,
        IReadOnlyCollection<(Combatant Combatant, GridPosition Position)> allies,
        IReadOnlyCollection<(Combatant Combatant, GridPosition Position)> enemies,
        DateTime createdAtUtc,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        if (allies.Count == 0)
            throw new DomainException("A tactical combat requires at least one ally.");
        if (enemies.Count == 0)
            throw new DomainException("A tactical combat requires at least one enemy.");
        if (allies.Count > Run.MaxPartySize)
            throw new DomainException(
                $"A tactical combat fields at most {Run.MaxPartySize} allies (got {allies.Count}).");

        var positions = new Dictionary<Guid, GridPosition>();

        foreach (var (combatant, position) in allies.Concat(enemies))
        {
            if (!battlefield.IsWalkable(position))
                throw new DomainException(
                    $"{combatant.DisplayName} cannot deploy on {position}: the cell is not walkable.");

            if (positions.ContainsValue(position))
                throw new DomainException(
                    $"{combatant.DisplayName} cannot deploy on {position}: the cell is already taken.");

            positions[combatant.Id.Value] = position;
        }

        return new TacticalCombat(
            id, runId, roomId, nodeId, battlefield,
            [.. allies.Select(a => a.Combatant)],
            [.. enemies.Select(e => e.Combatant)],
            positions,
            createdAtUtc)
        {
            HitCounterDoubleDamageEnabled = hitCounterDoubleDamageEnabled,
            FirstHitCriticalEnabled = firstHitCriticalEnabled,
            LowHpDamageAmplificationEnabled = lowHpDamageAmplificationEnabled,
            DotDurationExtensionTicks = dotDurationExtensionTicks,
            DuelDamageAsymmetryEnabled = duelDamageAsymmetryEnabled,
            DotMagnitudeBonus = dotMagnitudeBonus,
            HealingBlocked = healingBlocked,
        };
    }

    /// <summary>
    /// Reconstruit un combat tactique depuis sa forme persistée.
    /// </summary>
    /// <remarks>
    /// L'ordre d'initiative est <b>restauré</b>, pas recalculé : un combattant peut avoir vu sa
    /// Vitesse changer en cours de round (statut, Loi), et le recalculer ici ferait sauter son
    /// tour à quelqu'un simplement parce que la partie a été rechargée. L'ordre n'est refait
    /// qu'au passage au round suivant, comme en jeu.
    /// </remarks>
    public static TacticalCombat Rehydrate(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        TacticalBattlefield battlefield,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        IReadOnlyDictionary<Guid, GridPosition> positions,
        IReadOnlyDictionary<Guid, TacticalTurnState> turnStates,
        IReadOnlyList<Guid> initiativeOrder,
        int activeIndex,
        int roundNumber,
        CombatStatus status,
        DateTime createdAtUtc,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false,
        int hitCounter = 0,
        bool hasFirstHitLanded = false,
        int currentTick = 0,
        IEnumerable<string>? usedOnceSkillKeys = null)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(turnStates);
        ArgumentNullException.ThrowIfNull(initiativeOrder);

        var combat = new TacticalCombat(
            id, runId, roomId, nodeId, battlefield,
            [.. allies], [.. enemies],
            positions.ToDictionary(p => p.Key, p => p.Value),
            createdAtUtc)
        {
            Status = status,
            RoundNumber = roundNumber,
            HitCounterDoubleDamageEnabled = hitCounterDoubleDamageEnabled,
            FirstHitCriticalEnabled = firstHitCriticalEnabled,
            LowHpDamageAmplificationEnabled = lowHpDamageAmplificationEnabled,
            DotDurationExtensionTicks = dotDurationExtensionTicks,
            DuelDamageAsymmetryEnabled = duelDamageAsymmetryEnabled,
            DotMagnitudeBonus = dotMagnitudeBonus,
            HealingBlocked = healingBlocked,
            HitCounter = hitCounter,
            HasFirstHitLanded = hasFirstHitLanded,
            CurrentTick = currentTick,
        };

        combat._initiativeOrder = [.. initiativeOrder];
        combat._activeIndex = activeIndex;

        foreach (var (combatantId, turnState) in turnStates)
        {
            combat._turnStates[combatantId] = turnState;
        }

        foreach (var skillKey in usedOnceSkillKeys ?? [])
        {
            if (!string.IsNullOrWhiteSpace(skillKey))
                combat._usedOnceSkillKeys.Add(skillKey);
        }

        return combat;
    }

    //  Garde-fous 

    private void EnsureActive()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException($"This tactical combat is no longer active (status: {Status}).");
    }

    private bool IsDefeated(Guid combatantId) => RequireCombatant(combatantId).IsDefeated;

    private Combatant RequireCombatant(Guid combatantId) =>
        AllCombatants.FirstOrDefault(c => c.Id.Value == combatantId)
        ?? throw new DomainException($"Combatant '{combatantId}' is not part of this combat.");

    public GridPosition PositionOf(Guid combatantId) =>
        _positions.TryGetValue(combatantId, out var position)
            ? position
            : throw new DomainException($"Combatant '{combatantId}' is not on the battlefield.");
}
