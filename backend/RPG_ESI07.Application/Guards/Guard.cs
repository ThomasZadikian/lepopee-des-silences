namespace RPG_ESI07.Application.Guards;

/// <summary>
/// Guard clauses for defensive programming
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws if argument is null
    /// </summary>
    public static void ThrowIfNull<T>(T argument, string parameterName) where T : class
    {
        if (argument == null)
            throw new ArgumentNullException(parameterName);
    }

    /// <summary>
    /// Throws if string is null or empty
    /// </summary>
    public static void ThrowIfNullOrEmpty(string argument, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
    }

    /// <summary>
    /// Throws if collection is null or empty
    /// </summary>
    public static void ThrowIfNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
    {
        if (collection == null || !collection.Any())
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
    }

    /// <summary>
    /// Throws if number is negative
    /// </summary>
    public static void ThrowIfNegative(int value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentException($"{parameterName} cannot be negative", parameterName);
    }

    /// <summary>
    /// Throws if number is zero or negative
    /// </summary>
    public static void ThrowIfZeroOrNegative(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"{parameterName} must be greater than zero", parameterName);
    }

    /// <summary>
    /// Throws if condition is true
    /// </summary>
    public static void ThrowIf(bool condition, string message)
    {
        if (condition)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws if condition is false
    /// </summary>
    public static void ThrowIfNot(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws if collection is empty
    /// </summary>
    public static void ThrowIfEmpty<T>(IEnumerable<T> collection, string parameterName)
    {
        if (!collection?.Any() ?? true)
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
    }
}