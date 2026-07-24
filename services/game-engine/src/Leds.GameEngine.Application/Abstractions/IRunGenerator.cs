using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunGenerator
{
    string GeneratorVersion { get; }

    string MarkovMatrixVersion { get; }

    string GenerateSeed();

    Task<Room> GenerateInitialRoomAsync(
        string seed,
        CancellationToken cancellationToken = default);

    Task<Room> GenerateNextRoomAsync(Run run, CancellationToken cancellationToken = default);
}