namespace Leds.Player.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with id '{resourceId}' was not found.")
    {
    }
}
