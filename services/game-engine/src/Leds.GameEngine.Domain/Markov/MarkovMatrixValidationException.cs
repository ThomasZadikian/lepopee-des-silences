namespace Leds.GameEngine.Domain.Markov;

public sealed class MarkovMatrixValidationException : Exception
{
    public MarkovMatrixValidationException(string message)
        : base(message)
    {
    }
}