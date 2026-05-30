using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.Runs;

public sealed class Run
{
    private readonly List<Room> _rooms = [];

    private Run(
        RunId id,
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        RunStatus status,
        Room initialRoom,
        DateTimeOffset startedAt)
    {
        Id = id;
        PlayerId = playerId;
        Seed = seed;
        GeneratorVersion = generatorVersion;
        MarkovMatrixVersion = markovMatrixVersion;
        Status = status;
        CurrentRoomId = initialRoom.Id;
        StartedAt = startedAt;

        _rooms.Add(initialRoom);
    }

    public RunId Id { get; }

    public Guid PlayerId { get; }

    public string Seed { get; }

    public string GeneratorVersion { get; }

    public string MarkovMatrixVersion { get; }

    public RunStatus Status { get; private set; }

    public RoomId CurrentRoomId { get; private set; }

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
        DateTimeOffset startedAt)
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

        if (initialRoom.AvailableNodes.Count != 4)
        {
            throw new DomainException("A new run must start with exactly 4 available nodes.");
        }

        return new Run(
            RunId.New(),
            playerId,
            seed.Trim(),
            generatorVersion.Trim(),
            markovMatrixVersion.Trim(),
            RunStatus.Active,
            initialRoom,
            startedAt);
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

    public void AddNextNodesToCurrentRoom(IEnumerable<Node> nextNodes)
    {
        EnsureActive();

        CurrentRoom.AddNextNodes(nextNodes);
    }

    public void MoveToNextRoom(Room nextRoom)
    {
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

    public void Abandon(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Abandoned;
        EndedAt = endedAt;
    }

    private void EnsureActive()
    {
        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active.");
        }
    }
}