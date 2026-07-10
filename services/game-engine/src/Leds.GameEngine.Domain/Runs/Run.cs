using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.Runs;

public sealed class Run
{
    /// <summary>
    /// Base run-bag capacity (SFD "Système d'équipement et sac permanent" § 5) — raised by
    /// permanent backpacks equipped by the player, computed once at StartNew time and passed
    /// in like the other flattened starting stats (attack/defense/speed/focus).
    /// </summary>
    public const int DefaultRunItemCapacity = 6;

    private readonly List<Room> _rooms = [];
    private readonly List<ActivePalaceLaw> _activePalaceLaws = [];
    private readonly List<string> _memoryFragments = [];
    private readonly List<RunItem> _runItems = [];
    private readonly List<RunModifier> _runModifiers = [];
    private Combat? _activeCombat;
    private ActiveCurse? _activeCurse;
    private RunSnapshot? _roomSnapshot;
    private RunPlayerSnapshot? _playerSnapshot;
    private RunStatus? _preSuspendStatus;
    private readonly Dictionary<string, NpcRelationship> _npcRelationships = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeNpcKey;

    private sealed record RunSnapshot(
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        string[] MemoryFragments,
        ActivePalaceLaw[] ActivePalaceLaws,
        Guid[] RunItemIds,
        Guid[] RunModifierIds);

    public IReadOnlyCollection<ActivePalaceLaw> ActivePalaceLaws =>
    _activePalaceLaws.AsReadOnly();

    public IReadOnlyCollection<string> MemoryFragments =>
        _memoryFragments.AsReadOnly();

    public IReadOnlyCollection<RunItem> RunItems => _runItems.AsReadOnly();

    public int RunItemCapacity { get; }

    /// <summary>
    /// All run modifiers — both active and already-consumed.
    /// Filter by <see cref="RunModifier.IsConsumed"/> as needed.
    /// </summary>
    public IReadOnlyCollection<RunModifier> RunModifiers => _runModifiers.AsReadOnly();

    private Run(
        RunId id,
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        RunStatus status,
        Room initialRoom,
        DateTimeOffset startedAt,
        int maxHp,
        int currentHp,
        int attack,
        int defense,
        int speed,
        int focus,
        int currentRoomIndex = 0,
        CombatId? activeCombatId = null,
        RewardOfferId? pendingRewardOfferId = null,
        int runItemCapacity = DefaultRunItemCapacity,
        IReadOnlyDictionary<string, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0)
    {
        Id = id;
        PlayerId = playerId;
        Seed = seed;
        GeneratorVersion = generatorVersion;
        MarkovMatrixVersion = markovMatrixVersion;
        Status = status;
        CurrentRoomId = initialRoom.Id;
        StartedAt = startedAt;
        MaxHp = maxHp;
        CurrentHp = currentHp;
        Attack = attack;
        Defense = defense;
        Speed = speed;
        Focus = focus;
        CurrentRoomIndex = currentRoomIndex;
        ActiveCombatId = activeCombatId;
        PendingRewardOfferId = pendingRewardOfferId;
        RunItemCapacity = runItemCapacity;
        TypedDamageReductions = typedDamageReductions ?? new Dictionary<string, int>();
        HitChanceBonusPercent = hitChanceBonusPercent;
        DotDurationReductionPercent = dotDurationReductionPercent;
        DotDamageReductionPercent = dotDamageReductionPercent;
        MagicDamageBonusPercent = magicDamageBonusPercent;
        MagicDamageReductionPercent = magicDamageReductionPercent;
        CriticalChanceBonusPercent = criticalChanceBonusPercent;

        _rooms.Add(initialRoom);
    }

    public RunId Id { get; }

    public Guid PlayerId { get; }

    public string Seed { get; }

    public string GeneratorVersion { get; }

    public string MarkovMatrixVersion { get; }

    public RunStatus Status { get; private set; }

    public RoomId CurrentRoomId { get; private set; }

    public CombatId? ActiveCombatId { get; private set; }

    public bool HasActiveCombat => ActiveCombatId.HasValue || _activeCombat is not null;

    /// <summary>
    /// The active combat runtime domain object, if any.
    /// Set via <see cref="StartCombat"/>.
    /// </summary>
    public Combat? ActiveCombat => _activeCombat;

    public RewardOfferId? PendingRewardOfferId { get; private set; }

    public bool HasPendingRewardOffer => PendingRewardOfferId.HasValue;

    /// <summary>
    /// The active curse applied to the run, if any.
    /// </summary>
    public ActiveCurse? ActiveCurse => _activeCurse;

    /// <summary>
    /// Immutable player snapshot captured at run creation.
    /// Null only for runs created before data-model-0.1 migration.
    /// </summary>
    /// 
        // ── NPC encounters (npc-system-sfd-0.1) ──────────────────────────────────

    /// <summary>Key of the NPC currently being talked to, if any.</summary>
    public string? ActiveNpcKey => _activeNpcKey;

    /// <summary>All NPC relationships met during this run (L2 memory; L1 uses the active one).</summary>
    public IReadOnlyCollection<NpcRelationship> NpcRelationships =>
        _npcRelationships.Values.ToArray();

    public NpcRelationship? ActiveNpcRelationship =>
        _activeNpcKey is null ? null : GetNpcRelationship(_activeNpcKey);

    public NpcRelationship? GetNpcRelationship(string npcKey) =>
        _npcRelationships.TryGetValue(npcKey, out var relationship) ? relationship : null;

    /// <summary>
    /// Marks an NPC encounter active. A recurring NPC (already met) resumes its existing
    /// relationship and registers a new meeting; otherwise a fresh relationship begins.
    /// The active dialogue node is initialised lazily by the resolver from the NPC graph.
    /// </summary>
    public NpcRelationship BeginOrResumeNpcEncounter(string npcKey)
    {
        _activeNpcKey = npcKey;

        if (_npcRelationships.TryGetValue(npcKey, out var existing))
        {
            existing.RegisterNewMeeting();
            return existing;
        }

        var relationship = NpcRelationship.Begin(npcKey, entryNodeKey: null);
        _npcRelationships[npcKey] = relationship;
        return relationship;
    }

    public void EndNpcEncounter() => _activeNpcKey = null;

    /// <summary>Rehydration hook for persistence (Wave 5).</summary>
    public void RehydrateNpcRelationship(NpcRelationship relationship)
    {
        _npcRelationships[relationship.NpcKey] = relationship;
    }

    /// <summary>Rehydration hook for persistence (Wave 5).</summary>
    public void RehydrateActiveNpcKey(string? activeNpcKey)
    {
        _activeNpcKey = activeNpcKey;
    }
    public RunPlayerSnapshot? PlayerSnapshot => _playerSnapshot;

    /// <summary>
    /// Attaches a player snapshot to this run.
    /// Called once at run creation; will throw if already set.
    /// </summary>
    public void AttachPlayerSnapshot(RunPlayerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_playerSnapshot is not null)
        {
            throw new DomainException("Player snapshot is already attached to this run.");
        }

        _playerSnapshot = snapshot;
    }

    public PlayerRuntimeState PlayerState { get; private set; } = null!;

    public int MaxHp { get; }

    public int CurrentHp { get; private set; }

    public int Attack { get; private set; }

    public int Defense { get; private set; }

    public int Speed { get; private set; }

    /// <summary>
    /// Character Focus stat (drives critical-hit chance in combat). Immutable for
    /// the run; sourced from the main character at run start.
    /// </summary>
    public int Focus { get; }

    /// <summary>
    /// Equipment-driven typed damage reductions (EmotionalType name -> percent 0-100),
    /// computed once at run start from equipped items (SFD equipment §8) and immutable
    /// for the run's lifetime, same as Attack/Defense/Speed/Focus.
    /// </summary>
    public IReadOnlyDictionary<string, int> TypedDamageReductions { get; }

    /// <summary>
    /// Equipment-driven percentage points added to the protagonist's hit chance
    /// (e.g. Lunettes d'érudit: +10%). Computed once at run start, immutable for
    /// the run's lifetime, same as TypedDamageReductions.
    /// </summary>
    public int HitChanceBonusPercent { get; }

    /// <summary>
    /// Equipment-driven percentage (0-100) by which incoming DamageOverTime effects
    /// have their duration/per-tick damage reduced (e.g. Main de Khasma).
    /// </summary>
    public int DotDurationReductionPercent { get; }
    public int DotDamageReductionPercent { get; }

    /// <summary>
    /// Equipment-driven percentage points added to / subtracted from Magic-category
    /// skill damage (e.g. Pomenian's monocle). Computed once at run start, immutable
    /// for the run's lifetime, same as HitChanceBonusPercent.
    /// </summary>
    public int MagicDamageBonusPercent { get; }
    public int MagicDamageReductionPercent { get; }
    public int CriticalChanceBonusPercent { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// The timestamp at which the run was saved and exited by the player.
    /// Set when <see cref="SaveAndExit"/> is called.
    /// <c>null</c> when the run has never been suspended.
    /// </summary>
    public DateTimeOffset? SavedAt { get; private set; }

    /// <summary>
    /// Zero-based index of the current room in the infinite run sequence.
    /// Threshold is always index 0. Incremented by MoveToNextRoom (future).
    /// Display as <c>CurrentRoomIndex + 1</c> for player-facing "Salle N" labels.
    /// </summary>
    /// <remarks>
    /// Terminology guide:
    /// <list type="bullet">
    ///   <item><c>CurrentRoomIndex</c> — which room in the run (0, 1, 2 …)</item>
    ///   <item><c>Room.CurrentNodeDepth</c> — how far into the current RoomMap's rows</item>
    ///   <item><c>MapNode.Row</c> — a node's fixed position within the RoomMap layout</item>
    /// </list>
    /// Do not use <c>CurrentDepth</c> or <c>Room.Depth</c> to display room count to players.
    /// </remarks>
    public int CurrentRoomIndex { get; private set; }

    /// <summary>
    /// Depth of the current room within the run, derived from <see cref="Room.Depth"/>.
    /// Used internally for room-sequencing validation (e.g. <see cref="MoveToNextRoom"/>).
    /// Not intended as a player-facing room counter — use <see cref="CurrentRoomIndex"/> instead.
    /// </summary>
    public int CurrentDepth => CurrentRoom.Depth;

    public Room CurrentRoom => _rooms.Single(room => room.Id == CurrentRoomId);

    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    public static Run StartNew(
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        Room initialRoom,
        DateTimeOffset startedAt,
        int maxHp = 40,
        int currentHp = 40,
        int attack = 12,
        int defense = 6,
        int speed = 10,
        IReadOnlyCollection<PlayerRuntimeSkill>? playerSkills = null,
        int focus = 0,
        int runItemCapacity = DefaultRunItemCapacity,
        IReadOnlyDictionary<string, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0)
    {
        if (playerId == Guid.Empty)
        {
            throw new DomainException("Player id is required.");
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new DomainException("Run seed is required.");
        }

        if (string.IsNullOrWhiteSpace(generatorVersion))
        {
            throw new DomainException("Generator version is required.");
        }

        if (string.IsNullOrWhiteSpace(markovMatrixVersion))
        {
            throw new DomainException("Markov matrix version is required.");
        }

        if (initialRoom.Depth != 0)
        {
            throw new DomainException("Initial room depth must be 0.");
        }

        if (initialRoom.CurrentNodeDepth != 0)
        {
            throw new DomainException("Initial room node depth must be 0.");
        }

        if (initialRoom.TotalNodeCount is < 6 or > 30)
        {
            throw new DomainException("A new run must start with a room containing between 6 and 30 nodes.");
        }

        if (initialRoom.AvailableNodes.Count is < 1 or > 4)
        {
            throw new DomainException("A new run must start with between 1 and 4 available nodes.");
        }
        if (initialRoom.Nodes.Count(node => node.IsBoss) != 1)
        {
            throw new DomainException("A new run must start with exactly one room boss node.");
        }

        if (currentHp <= 0)
        {
            throw new DomainException("Current HP must be greater than 0.");
        }

        if (maxHp <= 0)
        {
            throw new DomainException("Max HP must be greater than 0.");
        }

        if (currentHp > maxHp)
        {
            throw new DomainException("Current HP cannot exceed max HP.");
        }

        if (attack <= 0)
        {
            throw new DomainException("Attack must be greater than 0.");
        }

        if (defense < 0)
        {
            throw new DomainException("Defense cannot be negative.");
        }

        if (speed <= 0)
        {
            throw new DomainException("Speed must be greater than 0.");
        }

        var run = new Run(
            RunId.New(),
            playerId,
            seed.Trim(),
            generatorVersion.Trim(),
            markovMatrixVersion.Trim(),
            RunStatus.Active,
            initialRoom,
            startedAt,
            maxHp,
            currentHp,
            attack,
            defense,
            speed,
            focus,
            runItemCapacity: runItemCapacity,
            typedDamageReductions: typedDamageReductions,
            hitChanceBonusPercent: hitChanceBonusPercent,
            dotDurationReductionPercent: dotDurationReductionPercent,
            dotDamageReductionPercent: dotDamageReductionPercent,
            magicDamageBonusPercent: magicDamageBonusPercent,
            magicDamageReductionPercent: magicDamageReductionPercent,
            criticalChanceBonusPercent: criticalChanceBonusPercent);

        run.PlayerState = PlayerRuntimeState.Create(
            maxVitality: maxHp,
            skills: playerSkills ?? CreateDefaultPlayerSkills(),
            currentVitality: currentHp);

        run._roomSnapshot = run.CreateSnapshot();

        return run;
    }

    private static IReadOnlyCollection<PlayerRuntimeSkill> CreateDefaultPlayerSkills()
    {
        return
        [
            PlayerRuntimeSkill.Create(
                key: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingType: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: 10),
            PlayerRuntimeSkill.Create(
                key: "skill.basic.guard",
                displayName: "Garde",
                skillType: "Defense",
                targetingType: "Self",
                effectType: "Guard",
                manaCost: 0,
                chargeCost: 0,
                basePower: 5)
        ];
    }

    public void ChooseNode(NodeId nodeId)
    {
        EnsureActive();

        CurrentRoom.SelectNode(nodeId);
    }

    public void ResolveCurrentEvent()
    {
        EnsureActive();

        CurrentRoom.ResolveSelectedNodeEvent();

        if (CurrentRoom.State == RoomState.Completed)
        {
            Status = RunStatus.RoomResolved;
        }
    }

    public void ProgressCurrentRoom()
    {
        EnsureActive();

        CurrentRoom.UnlockNextNodeLayer();
    }

    /// <summary>
    /// Transitions the run from <see cref="RunStatus.RoomResolved"/> (boss reward collected)
    /// to <see cref="RunStatus.Interlude"/>, where the player navigates the interlude
    /// hub before entering the next room.
    /// </summary>
    public void EnterInterlude()
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.RoomResolved)
        {
            throw new DomainException(
                "Cannot enter Interlude: run must be in RoomResolved (room cleared) state.");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot enter Interlude: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException(
                "Cannot enter Interlude: run has a pending reward offer that must be selected first.");
        }

        Status = RunStatus.Interlude;
    }

    /// <summary>
    /// Moves the run from <see cref="RunStatus.Interlude"/> to the next room.
    /// Increments <see cref="CurrentRoomIndex"/> and sets the run back to
    /// <see cref="RunStatus.Active"/>.
    /// </summary>
    public void MoveToNextRoom(Room nextRoom)
    {
        _roomSnapshot = CreateSnapshot();

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot move to the next room: run must be in Interlude state.");
        }

        if (nextRoom.Depth != CurrentDepth + 1)
        {
            throw new DomainException("Next room depth must be current depth + 1.");
        }

        // Run sans fin : aucune profondeur maximale. La room boss (Him'Lit) est portee
        // par son type de room, que le generateur produit tous les 10 rooms.
        _rooms.Add(nextRoom);
        CurrentRoomId = nextRoom.Id;
        CurrentRoomIndex++;
        Status = nextRoom.RoomType == RoomType.Final
            ? RunStatus.BossReached
            : RunStatus.Active;
    }

    public void CompleteRun(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Completed;
        EndedAt = endedAt;
    }

    public void FailRun(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    public void Abandon(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Abandoned;
        EndedAt = endedAt;
    }

    /// <summary>
    /// Suspends the run at a safe point (RoomResolved or Interlude) and returns the player
    /// to the main menu. The run can be resumed later.
    /// </summary>
    /// <remarks>
    /// Safe points: <see cref="RunStatus.RoomResolved"/> and <see cref="RunStatus.Interlude"/>.
    /// The run must have no active combat and no pending reward offer.
    /// </remarks>
    public void SaveAndExit(DateTimeOffset savedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Run is closed or already suspended.");
        }

        if (Status is not (RunStatus.RoomResolved or RunStatus.Interlude))
        {
            throw new DomainException(
                "Cannot save and exit: run must be at a safe point (RoomResolved or Interlude).");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot save and exit: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException(
                "Cannot save and exit: run has a pending reward offer that must be selected first.");
        }

        _preSuspendStatus = Status;
        Status = RunStatus.Suspended;
        SavedAt = savedAt;
    }

    /// <summary>
    /// Resumes the run from a suspended state, restoring the pre-suspend status
    /// (<see cref="RunStatus.RoomResolved"/> or <see cref="RunStatus.Interlude"/>)
    /// so the player can continue from where they left off.
    /// </summary>
    public void Resume()
    {
        if (Status != RunStatus.Suspended)
        {
            throw new DomainException("Cannot resume a run that is not suspended.");
        }

        if (_preSuspendStatus is null)
        {
            throw new DomainException(
                "Cannot resume: pre-suspend status is missing. The run may have been saved by an older version.");
        }

        var restoredStatus = _preSuspendStatus.Value;
        Status = restoredStatus;
        SavedAt = null;
        _preSuspendStatus = null;

        // Recreate a snapshot so the player can exit the room again after resuming.
        if (restoredStatus == RunStatus.Active)
        {
            _roomSnapshot = CreateSnapshot();
        }
    }

    public void SetActiveCombat(CombatId combatId)
    {
        if (combatId.Value == Guid.Empty)
        {
            throw new DomainException("Combat id is required.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active to set an active combat.");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Run already has an active combat.");
        }

        ActiveCombatId = combatId;
    }

    public void StartCombat(Combat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        if (combat.Id.Value == Guid.Empty)
        {
            throw new DomainException("Combat id is required.");
        }

        if (combat.RunId != Id)
        {
            throw new DomainException("Combat does not belong to this run.");
        }

        if (combat.Status != CombatStatus.Active)
        {
            throw new DomainException("Combat must be active to be started.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active to start a combat.");
        }

        if (_activeCombat is not null)
        {
            throw new DomainException("Run already has an active combat.");
        }

        if (ActiveCombatId.HasValue && ActiveCombatId.Value != combat.Id)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        ActiveCombatId = combat.Id;
        _activeCombat = combat;
    }

    public void CompleteActiveCombat(CombatId combatId)
    {
        if (!HasActiveCombat)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (ActiveCombatId != combatId)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        ActiveCombatId = null;
        _activeCombat = null;

        ResolveCurrentEvent();
    }

    public void CompleteActiveCombat()
    {
        if (_activeCombat is null)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (_activeCombat.Status != CombatStatus.Completed)
        {
            throw new DomainException("Active combat must be completed before resolving combat victory.");
        }

        ActiveCombatId = null;
        _activeCombat = null;

        ResolveCurrentEvent();
    }

    public void FailActiveCombat(DateTimeOffset endedAt)
    {
        if (_activeCombat is null)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (_activeCombat.Status != CombatStatus.Failed)
        {
            throw new DomainException("Active combat must be failed before resolving combat defeat.");
        }

        ActiveCombatId = null;
        _activeCombat = null;
        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    public void SetPendingRewardOffer(RewardOfferId rewardOfferId)
    {
        if (rewardOfferId.Value == Guid.Empty)
        {
            throw new DomainException("Reward offer id is required.");
        }

        if (Status != RunStatus.Active && Status != RunStatus.RoomResolved)
        {
            throw new DomainException("Run must be active or room resolved to set a pending reward offer.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException("Run already has a pending reward offer.");
        }

        PendingRewardOfferId = rewardOfferId;
    }

    public void ClearPendingRewardOffer()
    {
        if (!HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        PendingRewardOfferId = null;
    }

    public void ApplyHeal(int amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Heal amount must be positive.");
        }

        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// Out-of-combat vitality loss (e.g. an NPC consequence). Clamped to a minimum of 1
    /// so a map encounter never kills the player directly.
    /// </summary>
    public void ApplyVitalityLoss(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Math.Max(1, CurrentHp - amount);
        PlayerState?.LoseVitality(amount, floor: 1);
    }

    public void ApplyStatBonus(string stat, int value)
    {
        if (string.IsNullOrWhiteSpace(stat))
        {
            throw new DomainException("Stat name is required.");
        }

        switch (stat.Trim().ToLowerInvariant())
        {
            case "attack":
                Attack += value;
                break;

            case "defense":
                Defense += value;
                break;

            case "speed":
                Speed += value;
                break;

            case "all":
                Attack += value;
                Defense += value;
                break;

            default:
                throw new DomainException($"Unknown stat: '{stat}'.");
        }
    }

    public void AddMemoryFragment(string fragmentKey)
    {
        if (string.IsNullOrWhiteSpace(fragmentKey))
        {
            throw new DomainException("Memory fragment key is required.");
        }

        _memoryFragments.Add(fragmentKey.Trim());
    }

    public void ApplyReward(RewardChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Closed runs cannot receive rewards.");
        }

        if (!HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        switch (choice.RewardType)
        {
            case RewardType.Heal:
                var healAmount = ParsePayloadAmount(choice, "heal");
                ApplyHeal(healAmount);
                PlayerState.Heal(healAmount);
                break;

            case RewardType.TemporaryItem:
                var itemKey = choice.PayloadKey;
                AddRunItemFromPayload(itemKey);
                break;

            case RewardType.MemoryFragment:
                AddMemoryFragment(choice.PayloadKey);
                break;

            default:
                throw new DomainException($"Reward type '{choice.RewardType}' is not supported.");
        }
    }

    public void ApplyRewardEffect(RewardChoice choice)
    {
        ApplyReward(choice);
    }

    // Unconditional, unlimited add — used by reward selection (ApplyReward/EnrichLastAddedItem,
    // which assumes the just-added item is always _runItems.LastOrDefault()) and NPC-granted
    // offerings. Both are curated, bounded flows (a chosen reward option, an authored offer),
    // deliberately left outside the run-bag capacity check below — only exploration pickups
    // (TryAddRunItem) enforce RunItemCapacity (SFD "Équipement et sac permanent" § 5).
    public void AddRunItem(RunItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existing = _runItems.FirstOrDefault(i =>
            i.DefinitionKey == item.DefinitionKey &&
            i.Type == RunItemType.Consumable);

        if (existing is not null)
        {
            existing.AddQuantity(item.Quantity);
        }
        else
        {
            _runItems.Add(item);
        }

        ApplyItemGrantModifiers(item.EffectType, item.EffectAmount, item.DefinitionKey);
    }

    /// <summary>
    /// Capacity-aware add for exploration pickups. Merging into an existing stack never
    /// counts against capacity (it isn't a new distinct entry); a genuinely new item is
    /// rejected once <see cref="RunItemCapacity"/> distinct entries are already held.
    /// </summary>
    public bool TryAddRunItem(RunItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existing = _runItems.FirstOrDefault(i =>
            i.DefinitionKey == item.DefinitionKey &&
            i.Type == RunItemType.Consumable);

        if (existing is not null)
        {
            existing.AddQuantity(item.Quantity);
            return true;
        }

        if (_runItems.Count >= RunItemCapacity)
        {
            return false;
        }

        _runItems.Add(item);
        return true;
    }

    public void EnrichLastAddedItem(
        string definitionVersion,
        string? narrativeText,
        string category,
        string usageMode,
        string lifecycle,
        int maxStack,
        string? effectSetKey,
        bool isUsableInCombat,
        bool isUsableOutsideCombat,
        Guid sourceRewardOptionId,
        bool isContainer = false,
        int? containerCapacity = null,
        bool isLiquid = false)
    {
        var lastItem = _runItems.LastOrDefault();
        if (lastItem is null) return;

        var enriched = RunItem.Rehydrate(
            lastItem.Id,
            lastItem.DefinitionKey,
            lastItem.DisplayName,
            lastItem.Description,
            lastItem.Type,
            lastItem.Rarity,
            lastItem.Quantity,
            lastItem.EffectType,
            lastItem.EffectAmount,
            lastItem.CreatedAtUtc,
            definitionVersion: definitionVersion,
            narrativeText: narrativeText,
            category: category,
            usageMode: usageMode,
            lifecycle: lifecycle,
            maxStack: maxStack,
            effectSetKey: effectSetKey,
            effectSummary: lastItem.EffectSummary,
            isUsableInCombat: isUsableInCombat,
            isUsableOutsideCombat: isUsableOutsideCombat,
            sourceRewardOptionId: sourceRewardOptionId,
            isContainer: isContainer,
            containerCapacity: containerCapacity,
            isLiquid: isLiquid,
            containedLiquidDefinitionKey: lastItem.ContainedLiquidDefinitionKey);

        var index = _runItems.FindIndex(i => i.Id == lastItem.Id);
        if (index >= 0)
        {
            _runItems[index] = enriched;
        }
    }

    /// <summary>
    /// Pours a liquid item's contents into an empty container item, consuming one unit of the
    /// liquid from the run inventory. Both items must already be in the run's inventory.
    /// </summary>
    /// <returns>Whether the poured liquid item was fully consumed (quantity reached zero).</returns>
    public bool PourLiquid(RunItemId containerId, RunItemId liquidItemId)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot use items on a closed run.");

        var container = _runItems.FirstOrDefault(i => i.Id == containerId)
            ?? throw new DomainException($"Item '{containerId.Value}' not found in run inventory.");

        var liquidItem = _runItems.FirstOrDefault(i => i.Id == liquidItemId)
            ?? throw new DomainException($"Item '{liquidItemId.Value}' not found in run inventory.");

        if (!liquidItem.IsLiquid)
            throw new DomainException($"Item '{liquidItem.DefinitionKey}' is not a liquid.");

        container.PourLiquidInto(liquidItem.DefinitionKey);
        liquidItem.ConsumeOne();

        return liquidItem.Quantity == 0;
    }

    /// <summary>
    /// Empties a container item's contents, freeing it to receive a different liquid.
    /// </summary>
    public void EmptyContainer(RunItemId containerId)
    {
        var container = _runItems.FirstOrDefault(i => i.Id == containerId)
            ?? throw new DomainException($"Item '{containerId.Value}' not found in run inventory.");

        container.EmptyContents();
    }

    /// <summary>
    /// Adds a new modifier to the run. Duplicate (unconsumed) modifiers of the same type
    /// are allowed — they stack.
    /// </summary>
    public void AddRunModifier(RunModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot add a modifier to a closed run.");

        _runModifiers.Add(modifier);
    }

    /// <summary>
    /// Returns all unconsumed modifiers of the specified type.
    /// </summary>
    public IReadOnlyCollection<RunModifier> GetActiveModifiers(RunModifierType type)
        => _runModifiers
            .Where(m => m.Type == type && !m.IsConsumed)
            .ToArray();

    /// <summary>
    /// Consumes all unconsumed modifiers whose duration is <see cref="RunModifierDuration.NextCombatOnly"/>.
    /// Call this after a combat resolves.
    /// </summary>
    public void ConsumeNextCombatModifiers()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers
            .Where(m => m.Duration == RunModifierDuration.NextCombatOnly && !m.IsConsumed))
        {
            modifier.Consume(now);
        }
    }

    private void AddRunItemFromPayload(string payloadKey)
    {
        // Payload format: "item:<definitionKey>:<displayName>:<description>:<type>:<rarity>:<effectType>:<effectAmount>"
        var parts = payloadKey.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length < 8 || !string.Equals(parts[0], "item", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Invalid item reward payload format.");
        }

        var definitionKey = parts[1];
        var displayName = parts[2];
        var description = parts[3];
        var itemType = Enum.Parse<RunItemType>(parts[4], ignoreCase: true);
        var rarity = Enum.Parse<RunItemRarity>(parts[5], ignoreCase: true);
        var effectType = Enum.Parse<RunItemEffectType>(parts[6], ignoreCase: true);
        var effectAmount = int.Parse(parts[7]);

        var item = RunItem.Create(
            definitionKey,
            displayName,
            description,
            itemType,
            rarity,
            1,
            effectType,
            effectAmount);

        if (!TryAddRunItem(item))
        {
            throw new DomainException("Le sac est plein — il n'y a plus de place pour cet objet.");
        }

        ApplyItemGrantModifiers(effectType, effectAmount, definitionKey);
    }

    /// <summary>
    /// Translates certain RunItemEffectTypes into a permanent (until-run-ends)
    /// RunModifier at the moment the item is granted — whether picked up as loot
    /// (<see cref="AddRunItemFromPayload"/>) or given by an NPC (<see cref="AddRunItem"/>).
    /// </summary>
    private void ApplyItemGrantModifiers(RunItemEffectType effectType, int effectAmount, string definitionKey)
    {
        // Guard items create a permanent run-scoped StartingGuardBonus modifier.
        // The bonus stacks across multiple guard items but is capped at MaxStartingGuardBonus.
        if (effectType == RunItemEffectType.Guard && effectAmount > 0)
        {
            const int maxStartingGuardBonus = 30;
            var currentGuardBonus = _runModifiers
                .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
                .Sum(m => (int)m.Value);

            var cappedAmount = Math.Min(effectAmount, maxStartingGuardBonus - currentGuardBonus);
            if (cappedAmount > 0)
            {
                AddRunModifier(RunModifier.Create(
                    RunModifierType.StartingGuardBonus,
                    cappedAmount,
                    RunModifierDuration.UntilRunEnds,
                    "RunItem",
                    definitionKey));
            }
        }

        // Attack-type items override the hero's emotional attack type for the rest
        // of the run (effectAmount = int value of EmotionalType). Latest wins:
        // CombatFactory reads the most recent unconsumed override at combat creation.
        if (effectType == RunItemEffectType.AttackTypeOverride && effectAmount > 0)
        {
            AddRunModifier(RunModifier.Create(
                RunModifierType.AttackTypeOverride,
                effectAmount,
                RunModifierDuration.UntilRunEnds,
                "RunItem",
                definitionKey));
        }

        // Team-wide Speed bonus for as long as the item is held this run (e.g. Rêve
        // d'Erina: +5% team Speed). effectAmount is a whole percentage; stacks
        // additively with any other SpeedBonus modifier.
        if (effectType == RunItemEffectType.TeamSpeedBonus && effectAmount > 0)
        {
            AddRunModifier(RunModifier.Create(
                RunModifierType.SpeedBonus,
                effectAmount / 100.0,
                RunModifierDuration.UntilRunEnds,
                "RunItem",
                definitionKey));
        }
    }

    /// <summary>
    /// Applies a curse to the run, replacing any existing curse.
    /// </summary>
    public void ApplyCurse(ActiveCurse curse)
    {
        ArgumentNullException.ThrowIfNull(curse);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot apply a curse to a closed run.");

        _activeCurse = curse;
    }

    /// <summary>
    /// Clears the active curse after it has been consumed.
    /// </summary>
    public void ClearActiveCurse()
    {
        _activeCurse = null;
    }

    private static int ParsePayloadAmount(RewardChoice choice, string expectedPrefix)
    {
        var parts = choice.PayloadKey.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            !string.Equals(parts[0], expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[1], out var amount))
        {
            throw new DomainException($"Invalid payload for reward type '{choice.RewardType}'.");
        }

        return amount;
    }

    private RunSnapshot CreateSnapshot() => new(
        CurrentHp,
        Attack,
        Defense,
        Speed,
        [.. _memoryFragments],
        [.. _activePalaceLaws],
        [.. _runItems.Select(i => i.Id.Value)],
        [.. _runModifiers.Select(m => m.Id.Value)]);

    public CombatantSnapshot CreatePlayerSnapshot()
    {
        return CombatantSnapshot.Create(
            CombatantId.New(),
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: MaxHp,
            currentHealth: CurrentHp,
            attack: Attack,
            defense: Defense,
            speed: Speed);
    }

    public void FailActiveCombat(CombatId combatId, DateTimeOffset endedAt)
    {
        if (!HasActiveCombat)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (ActiveCombatId != combatId)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        ActiveCombatId = null;
        _activeCombat = null;
        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    private void EnsureActive()
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status == RunStatus.Suspended)
        {
            throw new DomainException("Run is suspended and cannot accept game actions until resumed.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active.");
        }
    }

    /// <summary>
    /// Exits the current room, rolling back all resources (HP, stats, memory
    /// fragments, active palace laws) to the state they were in when the room
    /// was first entered. The room is reset and the run is suspended so the
    /// player can resume later from the beginning of this room.
    /// </summary>
    public void ExitMidRoom(DateTimeOffset savedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Cannot exit mid-room: run must be active.");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot exit mid-room: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException("Cannot exit mid-room: run has a pending reward offer that must be selected first.");
        }

        if (_roomSnapshot is null)
        {
            throw new DomainException("Cannot exit mid-room: no room entry snapshot available.");
        }

        var snapshot = _roomSnapshot;
        _roomSnapshot = null;

        CurrentHp = snapshot.CurrentHp;
        Attack = snapshot.Attack;
        Defense = snapshot.Defense;
        Speed = snapshot.Speed;

        _memoryFragments.Clear();
        _memoryFragments.AddRange(snapshot.MemoryFragments);

        _activePalaceLaws.Clear();
        _activePalaceLaws.AddRange(snapshot.ActivePalaceLaws);

        var snapshotItemIds = snapshot.RunItemIds.ToHashSet();
        _runItems.RemoveAll(item => !snapshotItemIds.Contains(item.Id.Value));

        var snapshotModifierIds = snapshot.RunModifierIds.ToHashSet();
        _runModifiers.RemoveAll(mod => !snapshotModifierIds.Contains(mod.Id.Value));

        CurrentRoom.ResetProgress();

        _preSuspendStatus = RunStatus.Active;
        Status = RunStatus.Suspended;
        SavedAt = savedAt;
    }

    public void ActivatePalaceLaw(PalaceLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot activate a palace law on a closed run.");

        // Idempotent — same law key cannot be active twice.
        if (_activePalaceLaws.Any(activeLaw => activeLaw.Key == law.Key))
            return;

        if (law.Effects.Any(effect => effect.ModifierType == RunModifierType.RoomClimate))
        {
            ReplaceActiveRoomClimateLaws();
        }

        _activePalaceLaws.Add(ActivePalaceLaw.From(law));

        // Apply each mechanical effect of the law as a RunModifier.
        foreach (var effect in law.Effects)
        {
            var expiresAtRoomId = effect.Duration == RunModifierDuration.UntilRoomEnds
                ? CurrentRoomId.Value
                : (Guid?)null;

            AddRunModifier(RunModifier.Create(
                effect.ModifierType,
                effect.Value,
                effect.Duration,
                sourceType: "PalaceLaw",
                sourceKey: law.Key,
                expiresAtRoomId: expiresAtRoomId));
        }
    }

    private void ReplaceActiveRoomClimateLaws()
    {
        var replacedLawKeys = _runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                modifier.SourceType == "PalaceLaw" &&
                !modifier.IsConsumed)
            .Select(modifier => modifier.SourceKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (replacedLawKeys.Length == 0)
            return;

        var replacedLawKeySet = replacedLawKeys.ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            replacedLawKeySet.Contains(modifier.SourceKey) &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activePalaceLaws.RemoveAll(activeLaw => replacedLawKeySet.Contains(activeLaw.Key));
    }

    public void DebugPrepareForNextRoom()
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
            throw new DomainException("Run is closed.");

        if (Status == RunStatus.Interlude)
            return;

        ActiveCombatId = null;
        _activeCombat = null;
        PendingRewardOfferId = null;
        Status = RunStatus.Interlude;
    }

    public void DebugClearActivePalaceLaws()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activePalaceLaws.Clear();
    }

    public void DebugClearActiveCurse()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "Curse" &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activeCurse = null;
    }

    /// <summary>
    /// Uses a consumable item from the run inventory.
    /// Applies its effect to <see cref="PlayerState"/> and, if a combat is active,
    /// to the player combatant inside that combat.
    /// </summary>
    /// <returns>
    /// A tuple describing what was applied: the effect type, the amount, and whether
    /// the item was fully consumed (quantity reached zero).
    /// </returns>
    public (RunItemEffectType effectType, int amount, bool itemDepleted) UseItem(RunItemId itemId)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot use items on a closed run.");

        var item = _runItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item '{itemId.Value}' not found in run inventory.");

        // ConsumeOne validates Type == Consumable and Quantity > 0.
        item.ConsumeOne();

        ApplyItemEffectToPlayerState(item);

        if (_activeCombat is not null)
            ApplyItemEffectToCombatant(item, _activeCombat);

        var depleted = item.Quantity == 0;
        return (item.EffectType, item.EffectAmount, depleted);
    }

    private void ApplyItemEffectToPlayerState(RunItem item)
    {
        switch (item.EffectType)
        {
            case RunItemEffectType.Heal:
                PlayerState.Heal(item.EffectAmount);
                CurrentHp = PlayerState.CurrentVitality;
                break;

            case RunItemEffectType.Guard:
                PlayerState.GainGuard(item.EffectAmount);
                break;

            case RunItemEffectType.ManaRestore:
                PlayerState.GainMana(item.EffectAmount);
                break;

            case RunItemEffectType.ChargeRestore:
                PlayerState.GainCharge(item.EffectAmount);
                break;

            case RunItemEffectType.None:
            case RunItemEffectType.NextCombatGuard:
            case RunItemEffectType.NarrativeFragment:
                // These effect types are not manually activatable from inventory.
                throw new DomainException(
                    $"Item effect '{item.EffectType}' cannot be triggered manually from inventory.");

            default:
                throw new DomainException(
                    $"Unsupported item effect type: '{item.EffectType}'.");
        }
    }

    private static void ApplyItemEffectToCombatant(RunItem item, Combat combat)
    {
        var playerCombatant = combat.Allies
            .FirstOrDefault(a => a.Side == CombatantSide.Player);

        if (playerCombatant is null || playerCombatant.IsDefeated)
            return;

        switch (item.EffectType)
        {
            case RunItemEffectType.Heal:
                playerCombatant.ApplyHeal(item.EffectAmount);
                break;

            case RunItemEffectType.Guard:
                playerCombatant.GainGuard(item.EffectAmount);
                break;

            case RunItemEffectType.ManaRestore:
                playerCombatant.GainMana(item.EffectAmount);
                break;

            case RunItemEffectType.ChargeRestore:
                playerCombatant.GainCharge(item.EffectAmount);
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Rehydration (persistence restore)
    // -----------------------------------------------------------------------

    public RunSnapshotData? SnapshotData => _roomSnapshot is null
        ? null
        : new RunSnapshotData(
            _roomSnapshot.CurrentHp,
            _roomSnapshot.Attack,
            _roomSnapshot.Defense,
            _roomSnapshot.Speed,
            _roomSnapshot.MemoryFragments,
            _roomSnapshot.ActivePalaceLaws,
            _roomSnapshot.RunItemIds,
            _roomSnapshot.RunModifierIds);

    public RunStatus? PreSuspendStatus => _preSuspendStatus;

    public static Run Rehydrate(
        RunId id,
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        RunStatus status,
        RoomId currentRoomId,
        CombatId? activeCombatId,
        RewardOfferId? pendingRewardOfferId,
        int maxHp,
        int currentHp,
        int attack,
        int defense,
        int speed,
        int focus,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        DateTimeOffset? savedAt,
        int currentRoomIndex,
        IEnumerable<Room> rooms,
        IEnumerable<string> memoryFragments,
        IEnumerable<ActivePalaceLaw> activePalaceLaws,
        RunStatus? preSuspendStatus,
        RunSnapshotData? snapshot,
        Combat? activeCombat = null,
        PlayerRuntimeState? playerState = null,
        IEnumerable<RunItem>? runItems = null,
        IEnumerable<RunModifier>? runModifiers = null,
        RunPlayerSnapshot? playerSnapshot = null,
        ActiveCurse? activeCurse = null,
        int runItemCapacity = DefaultRunItemCapacity,
        IReadOnlyDictionary<string, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0)
    {
        var firstRoom = rooms.First();

        var run = new Run(id, playerId, seed, generatorVersion, markovMatrixVersion, status, firstRoom, startedAt, maxHp, currentHp, attack, defense, speed, focus, currentRoomIndex, activeCombatId, pendingRewardOfferId, runItemCapacity, typedDamageReductions, hitChanceBonusPercent, dotDurationReductionPercent, dotDamageReductionPercent, magicDamageBonusPercent, magicDamageReductionPercent, criticalChanceBonusPercent);
        foreach (var room in rooms.Skip(1))
        {
            run._rooms.Add(room);
        }

        run.CurrentRoomId = currentRoomId;
        run.EndedAt = endedAt;
        run.SavedAt = savedAt;
        run._preSuspendStatus = preSuspendStatus;
        run._memoryFragments.AddRange(memoryFragments);
        run._activePalaceLaws.AddRange(activePalaceLaws);
        run._activeCurse = activeCurse;

        if (snapshot is not null)
        {
            run._roomSnapshot = new RunSnapshot(
                snapshot.CurrentHp,
                snapshot.Attack,
                snapshot.Defense,
                snapshot.Speed,
                snapshot.MemoryFragments,
                snapshot.ActivePalaceLaws,
                snapshot.RunItemIds ?? [],
                snapshot.RunModifierIds ?? []);
        }

        run._activeCombat = activeCombat;

        if (runItems is not null)
        {
            run._runItems.AddRange(runItems);
        }

        if (runModifiers is not null)
        {
            run._runModifiers.AddRange(runModifiers);
        }

        run._playerSnapshot = playerSnapshot;

        run.PlayerState = playerState ?? PlayerRuntimeState.Create(
            maxVitality: maxHp,
            skills: CreateDefaultPlayerSkills(),
            currentVitality: currentHp);

        return run;
    }

    public sealed record RunSnapshotData(
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        string[] MemoryFragments,
        ActivePalaceLaw[] ActivePalaceLaws,
        Guid[]? RunItemIds = null,
        Guid[]? RunModifierIds = null);
}
