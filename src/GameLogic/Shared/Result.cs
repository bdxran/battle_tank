using System;

namespace BattleTank.GameLogic.Shared;

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    private readonly T _value;
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Result is a failure: {Error}");
    public string Error { get; }

    private Result(T value) { IsSuccess = true; _value = value; Error = string.Empty; }

    private Result(string error) { IsSuccess = false; _value = default!; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}
