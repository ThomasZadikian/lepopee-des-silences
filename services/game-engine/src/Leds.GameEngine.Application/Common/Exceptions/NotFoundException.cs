namespace Leds.GameEngine.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with id '{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public NotFoundException(string message)
        : base(message)
    {
        ResourceName = string.Empty;
        ResourceId = string.Empty;
    }

    public string ResourceName { get; }

    public object ResourceId { get; }
}