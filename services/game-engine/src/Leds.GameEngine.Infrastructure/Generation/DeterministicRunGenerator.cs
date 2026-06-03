using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Planning;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    private readonly ISeededRandomFactory _randomFactory;
    private readonly IRoomTypeResolver _roomTypeResolver;
    private readonly IRoomPlanGenerator _roomPlanGenerator;

    public DeterministicRunGenerator(
        ISeededRandomFactory randomFactory,
        IRoomTypeResolver roomTypeResolver,
        IRoomPlanGenerator roomPlanGenerator)
    {
        _randomFactory = randomFactory;
        _roomTypeResolver = roomTypeResolver;
        _roomPlanGenerator = roomPlanGenerator;
    }

    public string GeneratorVersion => "gen-0.4.0";

    public string MarkovMatrixVersion => StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

    private static string NodeEventTypeMarkovMatrixVersion =>
        StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion;

    public string GenerateSeed()
    {
        return $"seed-{Guid.NewGuid():N}";
    }

    public Room GenerateInitialRoom(string seed)
    {
        var random = _randomFactory.CreateForRoom(
            seed,
            roomDepth: 0,
            GeneratorVersion);

        return _roomPlanGenerator.Generate(
            seed,
            NodeEventTypeMarkovMatrixVersion,
            roomDepth: 0,
            roomType: RoomType.Threshold,
            random);
    }

    public Room GenerateNextRoom(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var nextRoomDepth = run.CurrentDepth + 1;

        var roomType = _roomTypeResolver.ResolveNextRoomType(
            run.Seed,
            nextRoomDepth,
            run.CurrentRoom.RoomType,
            MarkovMatrixVersion);

        var random = _randomFactory.CreateForRoom(
            run.Seed,
            nextRoomDepth,
            GeneratorVersion);

        return _roomPlanGenerator.Generate(
            run.Seed,
            NodeEventTypeMarkovMatrixVersion,
            nextRoomDepth,
            roomType,
            random);
    }
}