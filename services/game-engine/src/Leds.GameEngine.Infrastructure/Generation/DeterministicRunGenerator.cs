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

    public async Task<Room> GenerateInitialRoomAsync(
        string seed,
        CancellationToken cancellationToken = default)
    {
        var random = _randomFactory.CreateForRoom(
            seed,
            roomDepth: 0,
            GeneratorVersion);

        return await _mapRoomGenerator.GenerateAsync(
            seed,
            GeneratorVersion,
            roomDepth: 0,
            roomType: RoomType.Threshold,
            random,
            cancellationToken);
    }

    public async Task<Room> GenerateNextRoomAsync(
        Run run,
        CancellationToken cancellationToken = default)
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

        return await _mapRoomGenerator.GenerateAsync(
            run.Seed,
            GeneratorVersion,
            nextRoomDepth,
            roomType,
            random,
            cancellationToken);
    }
}
