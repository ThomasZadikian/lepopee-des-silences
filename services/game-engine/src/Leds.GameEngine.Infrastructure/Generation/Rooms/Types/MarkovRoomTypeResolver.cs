using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public sealed class MarkovRoomTypeResolver : IRoomTypeResolver
{
    private const int FinalRoomDepth = 10;
    private const string Scope = "room-type-generation";

    private readonly IRoomTypeMarkovMatrixProvider _matrixProvider;
    private readonly MarkovTransitionResolver _transitionResolver;

    public MarkovRoomTypeResolver(
        IRoomTypeMarkovMatrixProvider matrixProvider,
        MarkovTransitionResolver transitionResolver)
    {
        _matrixProvider = matrixProvider;
        _transitionResolver = transitionResolver;
    }

    public RoomType ResolveNextRoomType(
        string seed,
        int nextRoomDepth,
        RoomType currentRoomType,
        string matrixVersion)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed is required.", nameof(seed));
        }

        if (string.IsNullOrWhiteSpace(matrixVersion))
        {
            throw new ArgumentException("Matrix version is required.", nameof(matrixVersion));
        }

        if (nextRoomDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextRoomDepth),
                "Next room depth must be non-negative.");
        }

        if (nextRoomDepth == 0)
        {
            return RoomType.Threshold;
        }

        if (nextRoomDepth >= FinalRoomDepth)
        {
            return RoomType.Final;
        }

        if (currentRoomType == RoomType.Final)
        {
            throw new InvalidOperationException("Cannot resolve a next room type from the final room.");
        }

        var matrix = _matrixProvider.GetMatrix(matrixVersion);
        var currentState = MarkovState.Create(currentRoomType.ToString());

        var nextState = _transitionResolver.ResolveNextState(
            matrix,
            currentState,
            seed,
            Scope,
            nextRoomDepth);

        return ParseRoomType(nextState);
    }

    private static RoomType ParseRoomType(MarkovState state)
    {
        if (!Enum.TryParse<RoomType>(state.Value, ignoreCase: true, out var roomType))
        {
            throw new InvalidOperationException(
                $"Markov state '{state.Value}' cannot be mapped to a room type.");
        }

        if (roomType == RoomType.Final)
        {
            throw new InvalidOperationException(
                "Final room type cannot be selected by the room type Markov matrix.");
        }

        return roomType;
    }
}