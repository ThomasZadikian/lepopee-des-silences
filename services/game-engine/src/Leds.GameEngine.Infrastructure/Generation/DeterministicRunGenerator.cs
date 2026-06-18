using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.States;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.Infrastructure.Generation;

public sealed class DeterministicRunGenerator : IRunGenerator
{
    private readonly ISeededRandomFactory _randomFactory;
    private readonly IRoomTypeResolver _roomTypeResolver;
    private readonly IPalaceRoomStateResolver _palaceRoomStateResolver;
    private readonly IMapRoomGenerator _mapRoomGenerator;

    public DeterministicRunGenerator(
        ISeededRandomFactory randomFactory,
        IRoomTypeResolver roomTypeResolver,
        IPalaceRoomStateResolver palaceRoomStateResolver,
        IMapRoomGenerator mapRoomGenerator)
    {
        _randomFactory = randomFactory;
        _roomTypeResolver = roomTypeResolver;
        _palaceRoomStateResolver = palaceRoomStateResolver;
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
        var matrixVersion = string.IsNullOrWhiteSpace(run.MarkovMatrixVersion)
            ? MarkovMatrixVersion
            : run.MarkovMatrixVersion;

        var roomType = _roomTypeResolver.ResolveNextRoomType(
            run.Seed,
            nextRoomDepth,
            run.CurrentRoom.RoomType,
            matrixVersion);

        var palaceState = _palaceRoomStateResolver.ResolveNextState(
            new PalaceRoomStateResolutionContext(
                Seed: run.Seed,
                MatrixVersion: matrixVersion,
                PreviousRoomState: run.CurrentRoom.PalaceState,
                PreviousRoomType: run.CurrentRoom.RoomType,
                NextRoomType: roomType,
                NextRoomDepth: nextRoomDepth,
                ActiveLawKeys: run.ActivePalaceLaws
                    .Where(law => !law.IsConsumed)
                    .Select(law => law.Key)
                    .ToArray(),
                ActiveCurseKeys: run.ActiveCurse is { IsConsumed: false } activeCurse
                    ? [activeCurse.Key]
                    : [],
                ActiveClimate: ResolveActiveClimate(run)));

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
            cancellationToken,
            palaceState);
    }

    private static string? ResolveActiveClimate(Run run)
    {
        var modifier = run.RunModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == run.CurrentRoomId.Value)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => "Grey",
            2 => "Rain",
            3 => "Heatwave",
            4 => "Hail",
            _ => null
        };
    }
}
