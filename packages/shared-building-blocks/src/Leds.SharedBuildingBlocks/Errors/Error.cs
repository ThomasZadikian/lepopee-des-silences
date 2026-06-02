namespace Leds.SharedBuildingBlocks.Errors;

/// <summary>
/// Represents a stable, transportable error description.
/// This type is intentionally generic and must not contain game-specific concepts.
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; }

    public string Message { get; }

    private Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error Create(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        return new Error(code.Trim(), message.Trim());
    }

    public bool IsNone => this == None;
}