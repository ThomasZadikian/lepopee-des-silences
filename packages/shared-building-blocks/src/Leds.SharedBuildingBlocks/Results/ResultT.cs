using Leds.SharedBuildingBlocks.Errors;

namespace Leds.SharedBuildingBlocks.Results;

/// <summary>
/// Represents the result of an operation that can either return a value or fail.
/// </summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, Error.None)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access the value of a failed result.");
            }

            return _value!;
        }
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public new static Result<T> Failure(Error error)
    {
        return new Result<T>(error);
    }
}