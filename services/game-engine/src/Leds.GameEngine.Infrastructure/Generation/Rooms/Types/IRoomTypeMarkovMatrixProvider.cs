using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public interface IRoomTypeMarkovMatrixProvider
{
    MarkovTransitionMatrix GetMatrix(string matrixVersion);
}