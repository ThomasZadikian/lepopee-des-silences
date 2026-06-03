using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public sealed class StaticRoomTypeMarkovMatrixProvider : IRoomTypeMarkovMatrixProvider
{
    public const string MatrixKey = "room-type-transition";
    public const string SupportedVersion = "markov-room-type-0.1.0";

    public MarkovTransitionMatrix GetMatrix(string matrixVersion)
    {
        if (!string.Equals(matrixVersion, SupportedVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported room type Markov matrix version '{matrixVersion}'.");
        }

        var threshold = ToState(RoomType.Threshold);
        var memory = ToState(RoomType.Memory);
        var forest = ToState(RoomType.Forest);
        var rupture = ToState(RoomType.Rupture);
        var silence = ToState(RoomType.Silence);
        var antechamber = ToState(RoomType.Antechamber);

        var states = new[]
        {
            threshold,
            memory,
            forest,
            rupture,
            silence,
            antechamber
        };

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                threshold,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.25m,
                    [forest] = 0.25m,
                    [rupture] = 0.20m,
                    [silence] = 0.20m,
                    [antechamber] = 0.10m
                }),

            MarkovTransitionRow.Create(
                memory,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.25m,
                    [forest] = 0.20m,
                    [rupture] = 0.20m,
                    [silence] = 0.25m,
                    [antechamber] = 0.10m
                }),

            MarkovTransitionRow.Create(
                forest,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.25m,
                    [forest] = 0.25m,
                    [rupture] = 0.15m,
                    [silence] = 0.20m,
                    [antechamber] = 0.15m
                }),

            MarkovTransitionRow.Create(
                rupture,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.15m,
                    [forest] = 0.15m,
                    [rupture] = 0.30m,
                    [silence] = 0.20m,
                    [antechamber] = 0.20m
                }),

            MarkovTransitionRow.Create(
                silence,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.25m,
                    [forest] = 0.15m,
                    [rupture] = 0.20m,
                    [silence] = 0.30m,
                    [antechamber] = 0.10m
                }),

            MarkovTransitionRow.Create(
                antechamber,
                new Dictionary<MarkovState, decimal>
                {
                    [threshold] = 0.00m,
                    [memory] = 0.20m,
                    [forest] = 0.15m,
                    [rupture] = 0.25m,
                    [silence] = 0.20m,
                    [antechamber] = 0.20m
                })
        };

        return MarkovTransitionMatrix.Create(
            MatrixKey,
            SupportedVersion,
            states,
            rows);
    }

    private static MarkovState ToState(RoomType roomType)
    {
        return MarkovState.Create(roomType.ToString());
    }
}