namespace Algorist.Api.Application.Common;

/// <summary>Outcome of a use case: a value, or a stable error code from <c>ErrorCodes</c>.</summary>
public sealed class Result<T>
{
    private Result(T? value, string? errorCode)
    {
        Value = value;
        ErrorCode = errorCode;
    }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public bool IsSuccess => ErrorCode is null;

    public static Result<T> Ok(T value) => new(value, null);

    public static Result<T> Fail(string errorCode) => new(default, errorCode);

    public static implicit operator Result<T>(T value) => Ok(value);
}
