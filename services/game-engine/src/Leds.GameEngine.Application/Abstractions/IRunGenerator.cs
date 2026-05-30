using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunGenerator
{
    string GeneratorVersion { get; }

    string MarkovMatrixVersion { get; }

    string GenerateSeed();

    Room GenerateInitialRoom(string seed);
}