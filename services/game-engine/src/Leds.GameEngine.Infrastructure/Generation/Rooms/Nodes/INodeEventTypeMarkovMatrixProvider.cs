using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public interface INodeEventTypeMarkovMatrixProvider
{
    MarkovTransitionMatrix GetMatrix(string matrixVersion);
}