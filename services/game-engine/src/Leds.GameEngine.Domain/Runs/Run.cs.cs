using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.Runs;

public sealed class Run
{
    private readonly List<Room> _rooms = [];
    private readonly List<ActivePalaceLaw> _activePalaceLaws = [];
    private readonly List<string> _memoryFragments = [];
    private Combat? _activeCombat;
    private RunSnapshot? _roomSnapshot;
    private RunStatus? _preSuspendStatus;

    private sealed record RunSnapshot(
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        string[] MemoryFragments,
        ActivePalaceLaw[] ActivePalaceLaws);

    public IReadOnlyCollection<ActivePalaceLaw> ActivePalaceLaws =>
    _activePalaceLaws.AsReadOnly();

    public IReadOnlyCollection<string> MemoryFragments =>
        _memoryFragments.AsReadOnly();

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
        int currentRoomIndex = 0,
        CombatId? activeCombatId = null,
        RewardOfferId? pendingRewardOfferId = null)
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
        CurrentRoomIndex = currentRoomIndex;
        ActiveCombatId = activeCombatId;
        PendingRewardOfferId = pendingRewardOfferId;

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

    public int MaxHp { get; }

    public int CurrentHp { get; private set; }

    public int Attack { get; private set; }

    public int Defense { get; private set; }

    public int Speed { get; private set; }

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
        int speed = 10)
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
            speed);

        run._roomSnapshot = run.CreateSnapshot();

        return run;
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

        if (nextRoom.Depth > 10)
        {
            throw new DomainException("Run maximum depth is 10.");
        }

        _rooms.Add(nextRoom);
        CurrentRoomId = nextRoom.Id;
        CurrentRoomIndex++;
        Status = nextRoom.Depth == 10
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

    public void ApplyRewardEffect(RewardChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        switch (choice.RewardType)
        {
            case RewardType.Heal:
                var parts = choice.PayloadKey.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var healAmount))
                {
                    ApplyHeal(healAmount);
                }
                break;

            case RewardType.StatBonus:
                var statParts = choice.PayloadKey.Split(':');
                if (statParts.Length >= 3
                    && int.TryParse(statParts[2], out var statValue))
                {
                    ApplyStatBonus(statParts[1], statValue);
                }
                break;

            case RewardType.MemoryFragment:
                var fragmentParts = choice.PayloadKey.Split(':');
                var fragmentKey = fragmentParts.Length >= 2
                    ? fragmentParts[1]
                    : choice.PayloadKey;
                AddMemoryFragment(fragmentKey);
                break;

            default:
                throw new DomainException($"Reward type '{choice.RewardType}' is not supported.");
        }
    }

    private RunSnapshot CreateSnapshot() => new(
        CurrentHp,
        Attack,
        Defense,
        Speed,
        [.. _memoryFragments],
        [.. _activePalaceLaws]);

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

        CurrentRoom.ResetProgress();

        _preSuspendStatus = RunStatus.Active;
        Status = RunStatus.Suspended;
        SavedAt = savedAt;
    }

    public void ActivatePalaceLaw(PalaceLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Cannot activate a palace law on a closed run.");
        }

        if (_activePalaceLaws.Any(activeLaw => activeLaw.Key == law.Key))
        {
            return;
        }

        _activePalaceLaws.Add(ActivePalaceLaw.From(law));
    }
}
