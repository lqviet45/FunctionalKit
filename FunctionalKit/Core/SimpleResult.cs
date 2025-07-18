namespace FunctionalKit.Core;

/// <summary>
/// Simplified Result type using string for errors.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly Result<T, string> _inner;

    private Result(Result<T, string> inner)
    {
        _inner = inner;
    }

    public bool IsSuccess => _inner.IsSuccess;
    public bool IsFailure => _inner.IsFailure;
    public T Value => _inner.Value;
    public string Error => _inner.Error;

    public static Result<T> Success(T value) => new(Result<T, string>.Success(value));
    public static Result<T> Failure(string error) => new(Result<T, string>.Failure(error));

    public Result<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        new((_inner.Map(mapper)));

    public Result<TResult> FlatMap<TResult>(Func<T, Result<TResult>> mapper) =>
        IsSuccess ? mapper(_inner.Value) : Result<TResult>.Failure(_inner.Error);

    public T OrElse(T defaultValue) => _inner.OrElse(defaultValue);
    public T OrElse(Func<T> supplier) => _inner.OrElse(supplier);
    public T OrElseGet(Func<string, T> supplier) => _inner.OrElseGet(supplier);
    public T OrElseGet(Func<T> supplier) => _inner.OrElseGet(supplier);

    public Result<T> Match(Action<T> onSuccess, Action<string> onFailure)
    {
        _inner.Match(onSuccess, onFailure);
        return this;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        _inner.Match(onSuccess, onFailure);

    public Optional<T> ToOptional() => _inner.ToOptional();

    public bool Equals(Result<T> other) => _inner.Equals(other._inner);
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);
    public override int GetHashCode() => _inner.GetHashCode();
    public override string ToString() => _inner.ToString();

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}