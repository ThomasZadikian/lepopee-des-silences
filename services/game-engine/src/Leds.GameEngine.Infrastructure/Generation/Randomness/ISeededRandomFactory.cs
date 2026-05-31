namespace Leds.GameEngine.Infrastructure.Generation.Randomness;

public interface ISeededRandomFactory
{
    Random CreateForRoom(string seed, int roomDepth, string generatorVersion);
}