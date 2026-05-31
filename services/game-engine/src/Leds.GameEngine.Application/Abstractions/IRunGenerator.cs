using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunGenerator
{
    string GeneratorVersion { get; }

    string MarkovMatrixVersion { get; }

    string GenerateSeed();

    Room GenerateInitialRoom(string seed);
    Room GenerateNextRoom(Run run);

}