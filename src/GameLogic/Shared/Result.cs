namespace BattleTank.GameLogic.Shared;

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; Error = string.Empty; }

    // Value is default(T) when IsSuccess is false — callers must check IsSuccess before accessing Value.
    private Result(string error) { IsSuccess = false; Value = default!; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}
