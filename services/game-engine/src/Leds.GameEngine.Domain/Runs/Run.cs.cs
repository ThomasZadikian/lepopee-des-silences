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

    public bool HasActiveCombat => ActiveCombatId.HasValue;

    public RewardOfferId? PendingRewardOfferId { get; private set; }

    public bool HasPendingRewardOffer => PendingRewardOfferId.HasValue;

    public int MaxHp { get; }

    public int CurrentHp { get; private set; }

    public int Attack { get; private set; }

    public int Defense { get; private set; }

    public int Speed { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

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

        return new Run(
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

    public void MoveToNextRoom(Room nextRoom)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.RoomResolved || CurrentRoom.State != RoomState.Completed)
        {
            throw new DomainException("Current room must be completed before moving to the next room.");
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

        ResolveCurrentEvent();
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
        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    private void EnsureActive()
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active.");
        }
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