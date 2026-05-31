using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
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

    public string GeneratorVersion => "gen-0.2.0";

    public string MarkovMatrixVersion => "markov-0.2.0";

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
            roomDepth: 0,
            roomType: RoomType.Threshold,
            random);
    }

    public Room GenerateNextRoom(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var nextRoomDepth = run.CurrentDepth + 1;

        var random = _randomFactory.CreateForRoom(
            run.Seed,
            nextRoomDepth,
            GeneratorVersion);

        var roomType = _roomTypeResolver.Resolve(
            nextRoomDepth,
            random);

        return _roomPlanGenerator.Generate(
            nextRoomDepth,
            roomType,
            random);
    }
}