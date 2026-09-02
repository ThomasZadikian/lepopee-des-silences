namespace Leds.Player.Application.Common.Exceptions;

public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Authentication required.")
        : base(message)
    {
    }
}
