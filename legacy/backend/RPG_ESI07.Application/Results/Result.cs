namespace RPG_ESI07.Application.Results;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;

    protected Result() { }

    public static Result Success() => new() { IsSuccess = true };
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Ok(value);

    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<TValue> Failure<TValue>(string error) => Result<TValue>.Fail(error);
}

public class Result<TValue> : Result
{
    public TValue? Value { get; private set; }

    private Result() { }

    internal static Result<TValue> Ok(TValue value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    internal static Result<TValue> Fail(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public static implicit operator Result<TValue>(TValue value) => Ok(value);
    public static implicit operator Result<TValue>(string error) => Fail(error);
}