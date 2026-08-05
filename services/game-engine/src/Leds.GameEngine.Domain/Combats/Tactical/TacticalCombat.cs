using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
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
/// Implémente <see cref="ICombatContext"/> afin que le noyau de résolution — dégâts,
/// statuts, DoT et Lois — reste indépendant de l’interface et de la persistance.
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
    private readonly Dictionary<Guid, TacticalFacing> _facings;
    private readonly Dictionary<Guid, TacticalTurnState> _turnStates = new();
    private readonly Dictionary<(Guid CombatantId, string SkillKey), int> _skillCooldowns = new();
    private readonly Dictionary<Guid, int> _activationCounts = new();
    private readonly Dictionary<Guid, bool> _lastActivationUsedMagic = new();
    // Consumed by whichever application handler just called AdvanceToNextCombatant
    // (EndTacticalTurnCommandHandler, TacticalEnemyTurnDriver) to surface DoT/HoT ticks as
    // their own timeline event — see BeginActiveActivation. Overwritten every activation, so
    // it only ever reflects the most recent one; that is also the only one a single top-level
    // AdvanceToNextCombatant call ever reports back to.
    private Guid? _lastActivationCombatantId;
    private IReadOnlyCollection<StatusTickEvent> _lastActivationStatusTicks = [];
    private readonly HashSet<Guid> _cannotRevive = [];
    private readonly HashSet<string> _usedOnceSkillKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, HashSet<string>> _equippedItemKeys;
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
        DateTime createdAtUtc,
        Dictionary<Guid, TacticalFacing>? facings = null,
        GridPosition? escapePosition = null,
        RiskTier riskTier = RiskTier.Calme,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? equippedItemKeys = null)
    {
        Id = id;
        RunId = runId;
        RoomId = roomId;
        NodeId = nodeId;
        Battlefield = battlefield;
        _allies = allies;
        _enemies = enemies;
        _positions = positions;
        _facings = facings ?? positions.Keys.ToDictionary(id => id, _ => TacticalFacing.South);
        foreach (var combatantId in positions.Keys)
        {
            _activationCounts[combatantId] = 0;
            _lastActivationUsedMagic[combatantId] = false;
        }
        CreatedAtUtc = createdAtUtc;
        EscapePosition = escapePosition;
        RiskTier = riskTier;
        _equippedItemKeys = (equippedItemKeys
                ?? new Dictionary<Guid, IReadOnlyCollection<string>>())
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToHashSet(StringComparer.OrdinalIgnoreCase));
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
    public GridPosition? EscapePosition { get; }
    public bool CanEscape => EscapePosition.HasValue && Status == CombatStatus.Active;
    public RiskTier RiskTier { get; }
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> EquippedItemKeys =>
        _equippedItemKeys.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<string>)pair.Value.ToArray());

    public bool HasEquippedItem(Guid combatantId, string itemKey) =>
        _equippedItemKeys.TryGetValue(combatantId, out var keys)
        && keys.Contains(itemKey);
    public IReadOnlyDictionary<Guid, int> ActivationCounts => _activationCounts;
    public IReadOnlyDictionary<Guid, bool> LastActivationUsedMagic => _lastActivationUsedMagic;
    /// <summary>Who the most recent activation's DoT/HoT/guard ticks (if any) landed on.</summary>
    public Guid? LastActivationCombatantId => _lastActivationCombatantId;
    public IReadOnlyCollection<StatusTickEvent> LastActivationStatusTicks => _lastActivationStatusTicks;
    public IReadOnlySet<Guid> CannotRevive => _cannotRevive;
    public int StatusClockFor(Guid combatantId) =>
        _activationCounts.GetValueOrDefault(combatantId) * CombatTime.TicksPerTurn;

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

    public IReadOnlyDictionary<Guid, TacticalFacing> Facings => _facings;
    public IReadOnlyDictionary<Guid, GridPosition> Positions => _positions;

    public TacticalFacing FacingOf(Guid combatantId) =>
        _facings.TryGetValue(combatantId, out var facing)
            ? facing
            : throw new DomainException($"Combatant '{combatantId}' has no tactical facing.");

    public void OrientToward(Guid combatantId, GridPosition target)
    {
        var origin = PositionOf(combatantId);
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        if (dx == 0 && dy == 0)
            return;

        _facings[combatantId] = Math.Abs(dx) > Math.Abs(dy)
            ? (dx > 0 ? TacticalFacing.East : TacticalFacing.West)
            : (dy > 0 ? TacticalFacing.South : TacticalFacing.North);
    }

    public TacticalAttackArc AttackArcOf(Guid attackerId, Guid targetId)
    {
        var attacker = PositionOf(attackerId);
        var target = PositionOf(targetId);
        var dx = Math.Sign(attacker.X - target.X);
        var dy = Math.Sign(attacker.Y - target.Y);
        var (fx, fy) = FacingOf(targetId) switch
        {
            TacticalFacing.North => (0, -1),
            TacticalFacing.East => (1, 0),
            TacticalFacing.South => (0, 1),
            TacticalFacing.West => (-1, 0),
            _ => (0, 0)
        };
        var dot = (fx * dx) + (fy * dy);

        return dot switch
        {
            > 0 => TacticalAttackArc.Face,
            < 0 => TacticalAttackArc.Back,
            _ => TacticalAttackArc.Flank
        };
    }

    private void OrientInitialFormations()
    {
        foreach (var combatant in AllCombatants.Where(c => !c.IsDefeated))
        {
            var nearestOpponent = AllCombatants
                .Where(other => !other.IsDefeated && other.Side != combatant.Side)
                .OrderBy(other => PositionOf(combatant.Id.Value)
                    .ManhattanDistanceTo(PositionOf(other.Id.Value)))
                .ThenBy(other => other.Id.Value)
                .FirstOrDefault();

            if (nearestOpponent is not null)
                OrientToward(combatant.Id.Value, PositionOf(nearestOpponent.Id.Value));
        }
    }

    public void OrientTowardNearestVisibleEnemy(Guid combatantId)
    {
        var combatant = RequireCombatant(combatantId);
        var origin = PositionOf(combatantId);
        var nearest = AllCombatants
            .Where(other => !other.IsDefeated && other.Side != combatant.Side)
            .Where(other =>
            {
                var position = PositionOf(other.Id.Value);
                return Battlefield.ElevationAt(origin) > Battlefield.ElevationAt(position)
                    || TacticalMovement.HasLineOfSight(Battlefield, origin, position);
            })
            .OrderBy(other => origin.ManhattanDistanceTo(PositionOf(other.Id.Value)))
            .ThenBy(other => other.Id.Value)
            .FirstOrDefault();

        if (nearest is not null)
            OrientToward(combatantId, PositionOf(nearest.Id.Value));
    }

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

    public bool TryConsumeEquipmentTrigger(Guid combatantId, string triggerKey) =>
        _usedOnceSkillKeys.Add($"equipment-trigger:{combatantId:N}:{triggerKey}");

    public int RemainingCooldown(Guid combatantId, string skillKey) =>
        _skillCooldowns.GetValueOrDefault((combatantId, skillKey), 0);

    public IReadOnlyDictionary<string, int> CooldownsOf(Guid combatantId) =>
        _skillCooldowns
            .Where(pair => pair.Key.CombatantId == combatantId && pair.Value > 0)
            .ToDictionary(pair => pair.Key.SkillKey, pair => pair.Value, StringComparer.Ordinal);

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
    /// Recalcule immédiatement tout l'ordre après une variation de Vitesse effective.
    /// Le curseur repart au premier combattant et chaque activation redevient disponible :
    /// un combattant déjà joué peut donc rejouer, conformément au contrat canonique.
    /// </summary>
    public void RecalculateInitiativeAfterSpeedChange()
    {
        EnsureActive();
        _turnStates.Clear();
        RebuildInitiativeOrder();
        BeginActiveActivation();
    }

    public IReadOnlyDictionary<Guid, int> CaptureEffectiveSpeeds() =>
        AllCombatants.ToDictionary(c => c.Id.Value, c => c.EffectiveSpeed);

    public bool HaveEffectiveSpeedsChanged(IReadOnlyDictionary<Guid, int> before)
    {
        ArgumentNullException.ThrowIfNull(before);

        return AllCombatants.Any(c =>
            !before.TryGetValue(c.Id.Value, out var previous)
            || previous != c.EffectiveSpeed);
    }

    /// <summary>
    /// Réanime un allié sur la première case libre trouvée par anneaux autour de
    /// l'utilisateur, puis reconstruit immédiatement toute l'initiative.
    /// </summary>
    public GridPosition ReviveNear(Guid defeatedAllyId, Guid userId, int vitality)
    {
        EnsureActive();

        var ally = _allies.FirstOrDefault(c => c.Id.Value == defeatedAllyId)
            ?? throw new DomainException("Only an ally from this combat can be revived.");
        if (!ally.IsDefeated)
            throw new DomainException("The selected ally is not defeated.");
        if (_cannotRevive.Contains(defeatedAllyId))
            throw new DomainException("This ally cannot be revived during this combat.");

        var origin = PositionOf(userId);
        var occupied = OccupiedCells();
        var destination = Enumerable.Range(0, Battlefield.Height)
            .SelectMany(y => Enumerable.Range(0, Battlefield.Width)
                .Select(x => new GridPosition(x, y)))
            .Where(Battlefield.IsWalkable)
            .Where(cell => !occupied.Contains(cell))
            .Where(cell => cell != origin)
            .OrderBy(cell => origin.ManhattanDistanceTo(cell))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .FirstOrDefault();

        if (destination == default
            && (!Battlefield.IsWalkable(destination) || occupied.Contains(destination)))
        {
            throw new DomainException("No free cell is available to revive this ally.");
        }

        ally.Revive(vitality);
        _positions[defeatedAllyId] = destination;
        _turnStates.Clear();
        RebuildInitiativeOrder();
        BeginActiveActivation();
        return destination;
    }

    /// <summary>
    /// Appelé lorsqu'un combattant est vaincu pour le retirer immédiatement de l'ordre d'initiative.
    /// Cela évite les incohérences où un combattant mort pourrait encore avoir son tour.
    /// </summary>
    /// <param name="combatantId">L'ID du combattant vaincu.</param>
    public void OnCombatantDefeated(Guid combatantId)
    {
        var wasActive = ActiveCombatantId == combatantId;
        var defeated = RequireCombatant(combatantId);
        if (HasEquippedItem(combatantId, "item.diapason-audela"))
        {
            _cannotRevive.Add(combatantId);
            var recipientSide = defeated.Side;
            foreach (var ally in AllCombatants.Where(combatant =>
                         !combatant.IsDefeated && combatant.Side == recipientSide))
            {
                foreach (var stat in new[]
                         {
                             CombatStat.AttackPower,
                             CombatStat.Defense,
                             CombatStat.Speed,
                             CombatStat.Movement,
                             CombatStat.Focus,
                             CombatStat.MagicAttack,
                             CombatStat.MagicDefense
                         })
                {
                    ally.ApplyStatusEffect(CombatStatusEffect.Create(
                        $"equipment.diapason-audela.{stat}",
                        "Résonance de l’au-delà",
                        StatusEffectKind.StatModifier,
                        StatusClockFor(ally.Id.Value),
                        1,
                        magnitude: 10,
                        stat: stat,
                        isMagnitudePercentOfBaseStat: true,
                        isPermanent: true));
                }
            }
        }

        // Retirer le combattant de l'ordre d'initiative
        _initiativeOrder.Remove(combatantId);

        // Si c'était le combattant actif, avancer au suivant
        if (wasActive)
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

        if (ActiveCombatantId is { } outgoingId && !TurnStateOf(outgoingId).HasActed)
            OrientTowardNearestVisibleEnemy(outgoingId);

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

        BeginActiveActivation();
    }

    private void BeginNextRound()
    {
        RoundNumber++;
        _turnStates.Clear();
        RebuildInitiativeOrder();
        BeginActiveActivation();
    }

    /// <summary>
    /// Plafond de sécurité sur les réactivations enchaînées quand la Vitesse change en cours
    /// d'activation (un statut qui expire au tic peut faire bouger l'ordre). Un cas réel n'en
    /// redemande qu'une poignée ; ce garde-fou n'existe que pour qu'un statut mal réglé (Vitesse
    /// qui se réapplique elle-même à chaque tic, par exemple) échoue franchement au lieu de
    /// boucler indéfiniment dans une requête HTTP — même logique que
    /// <c>TacticalEnemyTurnDriver.MaxChainedEnemyTurns</c>.
    /// </summary>
    private const int MaxSpeedSettleRetries = 64;

    private void BeginActiveActivation()
    {
        Guid combatantId;
        Combatant combatant;
        var guard = 0;

        while (true)
        {
            if (ActiveCombatantId is not { } activeId)
                return;
            combatantId = activeId;

            _activationCounts[combatantId] = _activationCounts.GetValueOrDefault(combatantId) + 1;
            CurrentTick++;

            foreach (var key in _skillCooldowns.Keys
                         .Where(key => key.CombatantId == combatantId)
                         .ToArray())
            {
                var remaining = _skillCooldowns[key] - 1;
                if (remaining <= 0)
                    _skillCooldowns.Remove(key);
                else
                    _skillCooldowns[key] = remaining;
            }

            combatant = RequireCombatant(combatantId);
            var speedsBefore = CaptureEffectiveSpeeds();
            var wasAlive = !combatant.IsDefeated;
            var statusTicks = combatant.TickStatusEffects(StatusClockFor(combatantId));
            _lastActivationCombatantId = combatantId;
            _lastActivationStatusTicks = statusTicks;
            AwardPeriodicCharge(combatant, statusTicks, wasAlive);
            if (combatant.IsDefeated)
            {
                RegisterCombatantDefeated();
                OnCombatantDefeated(combatantId);
                CompleteIfAllEnemiesDefeated();
                FailIfAllAlliesDefeated();
                return;
            }

            if (!HaveEffectiveSpeedsChanged(speedsBefore))
                break;

            if (++guard > MaxSpeedSettleRetries)
                throw new InvalidOperationException(
                    "Tactical combatant activation did not settle after repeated speed changes; aborting to avoid an endless loop.");

            _turnStates.Clear();
            RebuildInitiativeOrder();
        }

        if (HasEquippedItem(combatantId, "item.gants-service-muet")
            && !_lastActivationUsedMagic.GetValueOrDefault(combatantId))
        {
            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                "equipment.gants-service-muet.conditional-defense",
                "Service muet",
                StatusEffectKind.StatModifier,
                StatusClockFor(combatantId),
                CombatTime.TicksPerTurn,
                magnitude: 10,
                stat: CombatStat.Defense,
                isMagnitudePercentOfBaseStat: true));
        }
        _lastActivationUsedMagic[combatantId] = false;

        if (!combatant.IsDefeated && combatant.MaxMana != int.MaxValue)
        {
            var regenerated = Math.Max(1, (int)Math.Floor(combatant.MaxMana * 0.05));
            combatant.GainMana(regenerated);
        }

        ApplyMetronomeIfActive();
        ApplyFalaiseWindIfActive();

        if (Status == CombatStatus.Active
            && ActiveCombatantId == combatantId
            && combatant.IsActivationBlocked)
        {
            ConsumeBasicAttackRestriction();
            MarkActiveCombatantActed();
            AdvanceToNextCombatant();
        }
    }

    private void ApplyMetronomeIfActive()
    {
        if (CurrentTick % 5 != 0
            || !_allies.Any(ally =>
                !ally.IsDefeated
                && HasEquippedItem(ally.Id.Value, "item.metronome-choeur")))
        {
            return;
        }

        var target = _enemies
            .Where(enemy => !enemy.IsDefeated)
            .OrderByDescending(enemy => enemy.EffectiveSpeed)
            .ThenBy(enemy => enemy.Id.Value)
            .FirstOrDefault();
        if (target is null)
            return;

        target.ApplyStatusEffect(CombatStatusEffect.Create(
            "equipment.metronome-choeur.silence",
            "Silence du métronome",
            StatusEffectKind.Silence,
            StatusClockFor(target.Id.Value),
            CombatTime.TicksPerTurn));
    }

    private void ApplyFalaiseWindIfActive()
    {
        if (!FalaiseWindEnabled
            || DeterministicCombatRoll.UnitInterval(
                $"falaise|{Id.Value:N}|{CurrentTick}") >= 0.10)
        {
            return;
        }

        var living = AllCombatants
            .Where(candidate => !candidate.IsDefeated)
            .OrderBy(candidate => candidate.Id.Value)
            .ToArray();
        if (living.Length == 0)
            return;

        var sample = DeterministicCombatRoll.UnitInterval(
            $"falaise-cible|{Id.Value:N}|{CurrentTick}");
        var pushed = living[Math.Min(living.Length - 1, (int)(sample * living.Length))];
        var position = PositionOf(pushed.Id.Value);
        var direction = new[]
            {
                (Dx: -1, Dy: 0, Distance: position.X),
                (Dx: 1, Dy: 0, Distance: Battlefield.Width - 1 - position.X),
                (Dx: 0, Dy: -1, Distance: position.Y),
                (Dx: 0, Dy: 1, Distance: Battlefield.Height - 1 - position.Y)
            }
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Dy)
            .ThenBy(candidate => candidate.Dx)
            .First();

        ForceMove(pushed.Id.Value, direction.Dx, direction.Dy, 1);
        if (!pushed.IsDefeated)
            return;

        RegisterCombatantDefeated();
        OnCombatantDefeated(pushed.Id.Value);
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();
    }

    private void AwardPeriodicCharge(
        Combatant holder,
        IReadOnlyCollection<StatusTickEvent> ticks,
        bool wasAlive)
    {
        foreach (var tick in ticks.Where(tick => !tick.Expired && tick.Amount > 0))
        {
            var sources = tick.StackSourceIds
                .Where(sourceId => sourceId.HasValue && sourceId.Value != holder.Id.Value)
                .Select(sourceId => sourceId!.Value)
                .ToArray();

            if (sources.Length > 0)
            {
                var total = 0.3m + (0.1m * Math.Max(0, tick.StackCount - 1));
                foreach (var group in sources.GroupBy(sourceId => sourceId))
                {
                    var source = AllCombatants.FirstOrDefault(
                        candidate => candidate.Id.Value == group.Key);
                    if (source is null)
                        continue;

                    var share = Math.Round(
                        total * group.Count() / tick.StackCount,
                        1,
                        MidpointRounding.AwayFromZero);
                    source.GainCharge(share);
                }
            }

            if (tick.Kind == StatusEffectKind.DamageOverTime
                && !holder.IsDefeated)
            {
                holder.GainCharge(0.3m);
            }
        }

        if (wasAlive && holder.IsDefeated)
        {
            var firstSourceId = ticks
                .FirstOrDefault(tick =>
                    !tick.Expired
                    && tick.Amount > 0
                    && tick.Kind == StatusEffectKind.DamageOverTime)
                ?.FirstStackSourceId;
            if (firstSourceId.HasValue)
            {
                AllCombatants.FirstOrDefault(candidate =>
                        candidate.Id.Value == firstSourceId.Value)
                    ?.GainCharge(0.3m);
            }
        }
    }

    /// <summary>Ce qu'a coûté un déplacement, et par où il est passé.</summary>
    public sealed record TacticalMoveResult(int Cost, IReadOnlyList<GridPosition> Path);
    public sealed record TacticalForcedMoveResult(
        IReadOnlyList<GridPosition> Path,
        int CollisionDamage,
        int FallDamage,
        bool EliminatedByVoid);

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
        var traversableAllies = AllCombatants
            .Where(c => !c.IsDefeated && c.Id.Value != combatantId && c.Side == combatant.Side)
            .Select(c => PositionOf(c.Id.Value))
            .ToHashSet();

        var reachable = TacticalMovement.ReachableCells(
            Battlefield,
            origin,
            TacticalMovement.BudgetFor(combatant.HasTacticalSlow
                ? Math.Max(1, combatant.EffectiveMovement / 2)
                : combatant.EffectiveMovement),
            occupied,
            traversableAllies);

        if (!reachable.TryGetValue(destination, out var cost))
            throw new DomainException($"{destination} is out of this combatant's movement range.");

        // Le chemin est renvoyé avec le coût : le client doit pouvoir montrer le trajet réel,
        // qui contourne murs et montées, plutôt que de faire glisser la figure en ligne droite.
        var path = TacticalMovement.PathTo(
            Battlefield,
            origin,
            destination,
            TacticalMovement.BudgetFor(combatant.HasTacticalSlow
                ? Math.Max(1, combatant.EffectiveMovement / 2)
                : combatant.EffectiveMovement),
            occupied,
            traversableAllies) ?? [destination];

        _positions[combatantId] = destination;
        _turnStates[combatantId] = state with { HasMoved = true };

        // Une seule unité alliée vivante suffit à évacuer le groupe. La fuite est immédiate à
        // l'arrivée : aucun ennemi ne reçoit un tour supplémentaire après que la sortie est
        // atteinte.
        if (combatant.Side == CombatantSide.Player && EscapePosition == destination)
            Status = CombatStatus.Escaped;

        return new TacticalMoveResult(cost, path);
    }

    public TacticalForcedMoveResult ForceMove(
        Guid combatantId,
        int deltaX,
        int deltaY,
        int distance)
    {
        EnsureActive();
        if (Math.Abs(deltaX) + Math.Abs(deltaY) != 1)
            throw new DomainException("Forced movement direction must be orthogonal.");
        if (distance < 1)
            throw new DomainException("Forced movement distance must be positive.");

        var pushed = RequireCombatant(combatantId);
        if (pushed.IsDefeated)
            throw new DomainException("A defeated combatant cannot be pushed.");

        var current = PositionOf(combatantId);
        var path = new List<GridPosition>();
        var fallDamage = 0;

        for (var step = 0; step < distance; step++)
        {
            var next = new GridPosition(current.X + deltaX, current.Y + deltaY);
            if (!Battlefield.Contains(next) || !Battlefield.IsFloor(next))
            {
                pushed.MarkDefeated();
                return new TacticalForcedMoveResult(path, 0, fallDamage, true);
            }

            var occupant = AllCombatants.FirstOrDefault(c =>
                !c.IsDefeated
                && c.Id.Value != combatantId
                && PositionOf(c.Id.Value) == next);
            if (!Battlefield.IsWalkable(next) || occupant is not null)
            {
                var remaining = distance - step;
                var collisionDamage = EnvironmentalDamage(pushed.MaxVitality, remaining);
                ApplyEnvironmentalDamage(pushed, collisionDamage);
                if (occupant is not null)
                    ApplyEnvironmentalDamage(occupant, Round(collisionDamage / 2.0));

                return new TacticalForcedMoveResult(
                    path,
                    collisionDamage,
                    fallDamage,
                    false);
            }

            var descendedLevels = Math.Max(
                0,
                Battlefield.ElevationAt(current) - Battlefield.ElevationAt(next));
            if (descendedLevels > 0)
            {
                var stepFallDamage = EnvironmentalDamage(
                    pushed.MaxVitality,
                    descendedLevels);
                fallDamage += stepFallDamage;
                ApplyEnvironmentalDamage(pushed, stepFallDamage);
                if (pushed.IsDefeated)
                    return new TacticalForcedMoveResult(path, 0, fallDamage, false);
            }

            current = next;
            _positions[combatantId] = current;
            path.Add(current);
        }

        return new TacticalForcedMoveResult(path, 0, fallDamage, false);
    }

    private static int EnvironmentalDamage(int maxVitality, int units) =>
        Round(maxVitality * 0.05 * units);

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static void ApplyEnvironmentalDamage(Combatant combatant, int amount)
    {
        if (amount > 0 && !combatant.IsDefeated)
            combatant.ApplyDamage(amount);
    }

    /// <summary>
    /// Marque l'action du combattant actif comme dépensée. Le noyau de résolution a déjà fait le
    /// travail : cet agrégat n'enregistre que la consommation.
    /// </summary>
    public void MarkActiveCombatantActed(CombatantSkill? usedSkill = null)
    {
        EnsureActive();

        var combatantId = ActiveCombatantId
            ?? throw new DomainException("No combatant is currently active.");

        var state = TurnStateOf(combatantId);
        if (state.HasActed)
            throw new DomainException("This combatant has already acted this turn.");

        _turnStates[combatantId] = state with { HasActed = true };
        RequireCombatant(combatantId).MarkActedThisCombat();
        _lastActivationUsedMagic[combatantId] = string.Equals(
            usedSkill?.Category,
            "Magic",
            StringComparison.OrdinalIgnoreCase);

        if (usedSkill is { Cooldown: > 0 })
            _skillCooldowns[(combatantId, usedSkill.Key)] = usedSkill.Cooldown;
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
    public bool PostDeathBasicAttackOnlyEnabled { get; private init; }
    public bool NextActionRestrictedToBasicAttack { get; private set; }
    public bool TapisPropreEnabled { get; private init; }
    public bool ThirdCupHealCorruptionEnabled { get; private init; }
    public bool FalaiseWindEnabled { get; private init; }
    public bool PresentationsEnabled { get; private init; }
    public bool MiroirEnabled { get; private init; }
    public bool HasMirrorTriggered { get; private set; }
    public string? ForgottenSkillKey { get; private init; }
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
        return HitCounterDoubleDamageEnabled && HitCounter % CombatRules.HitCounterTrigger == 0;
    }

    public bool TryConsumeFirstHitCritical()
    {
        if (HasFirstHitLanded)
            return false;

        HasFirstHitLanded = true;
        return FirstHitCriticalEnabled;
    }

    /// <inheritdoc />
    /// <remarks>Arme la restriction du prochain combattant dans l'ordre d'initiative.</remarks>
    public void RegisterCombatantDefeated()
    {
        if (PostDeathBasicAttackOnlyEnabled)
            NextActionRestrictedToBasicAttack = true;
    }

    public void ConsumeBasicAttackRestriction() =>
        NextActionRestrictedToBasicAttack = false;

    public bool TryConsumeMirrorTrigger(Combatant caster)
    {
        if (!MiroirEnabled || HasMirrorTriggered || caster.Side != CombatantSide.Player)
            return false;

        HasMirrorTriggered = true;
        return true;
    }

    public Combatant? GetFastestLivingEnemy() => _enemies
        .Where(enemy => !enemy.IsDefeated)
        .OrderByDescending(enemy => enemy.EffectiveSpeed)
        .ThenBy(enemy => enemy.Id.Value)
        .FirstOrDefault();

    public (int HealAmount, bool Triggered) ApplyThirdCupRollIfActive(Combatant target, int healAmount)
    {
        if (!ThirdCupHealCorruptionEnabled)
            return (healAmount, false);

        var seed = $"troisieme-tasse|{Id.Value:N}|{target.Id.Value:N}|{CurrentTick}";
        if (DeterministicCombatRoll.UnitInterval(seed) >= 0.10)
            return (healAmount, false);

        target.ApplyStatusEffect(CombatStatusEffect.Create(
            "troisieme-tasse-poison",
            "Poison léger (Troisième Tasse)",
            StatusEffectKind.DamageOverTime,
            StatusClockFor(target.Id.Value),
            CombatTime.TicksPerTurn * 4,
            magnitude: 3,
            tickInterval: CombatTime.TicksPerTurn));

        return (Math.Max(
            1,
            (int)Math.Round(healAmount * 0.5, MidpointRounding.AwayFromZero)), true);
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
        bool healingBlocked = false,
        bool postDeathBasicAttackOnlyEnabled = false,
        bool tapisPropreEnabled = false,
        bool thirdCupHealCorruptionEnabled = false,
        GridPosition? escapePosition = null,
        RiskTier riskTier = RiskTier.Calme,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? equippedItemKeys = null,
        bool falaiseWindEnabled = false,
        bool presentationsEnabled = false,
        bool miroirEnabled = false,
        string? forgottenSkillKey = null)
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

        var combat = new TacticalCombat(
            id, runId, roomId, nodeId, battlefield,
            [.. allies.Select(a => a.Combatant)],
            [.. enemies.Select(e => e.Combatant)],
            positions,
            createdAtUtc,
            escapePosition: escapePosition,
            riskTier: riskTier,
            equippedItemKeys: equippedItemKeys)
        {
            HitCounterDoubleDamageEnabled = hitCounterDoubleDamageEnabled,
            FirstHitCriticalEnabled = firstHitCriticalEnabled,
            LowHpDamageAmplificationEnabled = lowHpDamageAmplificationEnabled,
            DotDurationExtensionTicks = dotDurationExtensionTicks,
            DuelDamageAsymmetryEnabled = duelDamageAsymmetryEnabled,
            DotMagnitudeBonus = dotMagnitudeBonus,
            HealingBlocked = healingBlocked,
            PostDeathBasicAttackOnlyEnabled = postDeathBasicAttackOnlyEnabled,
            TapisPropreEnabled = tapisPropreEnabled,
            ThirdCupHealCorruptionEnabled = thirdCupHealCorruptionEnabled,
            FalaiseWindEnabled = falaiseWindEnabled,
            PresentationsEnabled = presentationsEnabled,
            MiroirEnabled = miroirEnabled,
            ForgottenSkillKey = forgottenSkillKey,
        };
        combat.OrientInitialFormations();
        combat.BeginActiveActivation();
        return combat;
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
        bool postDeathBasicAttackOnlyEnabled = false,
        bool nextActionRestrictedToBasicAttack = false,
        bool tapisPropreEnabled = false,
        bool thirdCupHealCorruptionEnabled = false,
        int hitCounter = 0,
        bool hasFirstHitLanded = false,
        int currentTick = 0,
        IEnumerable<string>? usedOnceSkillKeys = null,
        IReadOnlyDictionary<Guid, TacticalFacing>? facings = null,
        IReadOnlyDictionary<(Guid CombatantId, string SkillKey), int>? skillCooldowns = null,
        GridPosition? escapePosition = null,
        RiskTier riskTier = RiskTier.Calme,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? equippedItemKeys = null,
        IReadOnlyDictionary<Guid, int>? activationCounts = null,
        IReadOnlyDictionary<Guid, bool>? lastActivationUsedMagic = null,
        IReadOnlyCollection<Guid>? cannotRevive = null,
        bool falaiseWindEnabled = false,
        bool presentationsEnabled = false,
        bool miroirEnabled = false,
        bool hasMirrorTriggered = false,
        string? forgottenSkillKey = null)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(turnStates);
        ArgumentNullException.ThrowIfNull(initiativeOrder);

        var combat = new TacticalCombat(
            id, runId, roomId, nodeId, battlefield,
            [.. allies], [.. enemies],
            positions.ToDictionary(p => p.Key, p => p.Value),
            createdAtUtc,
            facings?.ToDictionary(pair => pair.Key, pair => pair.Value),
            escapePosition,
            riskTier,
            equippedItemKeys)
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
            PostDeathBasicAttackOnlyEnabled = postDeathBasicAttackOnlyEnabled,
            NextActionRestrictedToBasicAttack = nextActionRestrictedToBasicAttack,
            TapisPropreEnabled = tapisPropreEnabled,
            ThirdCupHealCorruptionEnabled = thirdCupHealCorruptionEnabled,
            FalaiseWindEnabled = falaiseWindEnabled,
            PresentationsEnabled = presentationsEnabled,
            MiroirEnabled = miroirEnabled,
            HasMirrorTriggered = hasMirrorTriggered,
            ForgottenSkillKey = forgottenSkillKey,
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

        foreach (var (key, remaining) in skillCooldowns
                     ?? new Dictionary<(Guid CombatantId, string SkillKey), int>())
        {
            if (remaining > 0)
                combat._skillCooldowns[key] = remaining;
        }

        foreach (var combatantId in combat._positions.Keys)
        {
            combat._activationCounts[combatantId] = Math.Max(
                0,
                activationCounts?.GetValueOrDefault(combatantId) ?? 0);
            combat._lastActivationUsedMagic[combatantId] =
                lastActivationUsedMagic?.GetValueOrDefault(combatantId) ?? false;
        }

        foreach (var combatantId in cannotRevive ?? [])
            combat._cannotRevive.Add(combatantId);

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
