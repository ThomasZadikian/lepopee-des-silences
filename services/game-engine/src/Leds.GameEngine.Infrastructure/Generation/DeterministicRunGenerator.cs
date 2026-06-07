using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    private readonly ISeededRandomFactory _randomFactory;
    private readonly IRoomTypeResolver _roomTypeResolver;
    private readonly IMapRoomGenerator _mapRoomGenerator;

    public DeterministicRunGenerator(
        ISeededRandomFactory randomFactory,
        IRoomTypeResolver roomTypeResolver,
        IMapRoomGenerator mapRoomGenerator)
    {
        _randomFactory = randomFactory;
        _roomTypeResolver = roomTypeResolver;
        _mapRoomGenerator = mapRoomGenerator;
    }

    public string GeneratorVersion => DefaultRoomMapLayoutTemplates.GeneratorVersion;

    public string MarkovMatrixVersion => StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

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

        return _mapRoomGenerator.Generate(
            seed,
            GeneratorVersion,
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

        return _mapRoomGenerator.Generate(
            run.Seed,
            GeneratorVersion,
            nextRoomDepth,
            roomType,
            random);
    }
}