namespace FunctionalKit.Core;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
/// <typeparam name="TError">The type of the error</typeparam>
public readonly struct Result<T, TError> : IEquatable<Result<T, TError>>
{
    private readonly T _value;
    private readonly TError _error;
    private readonly bool _isSuccess;

    private Result(T value, TError error, bool isSuccess)
    {
        _value = value;
        _error = error;
        _isSuccess = isSuccess;
    }

    /// <summary>
    /// Gets a value indicating whether the result represents a success.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the result represents a failure.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the success value. Throws if the result is a failure.
    /// </summary>
    public T Value
    {
        get
        {
            if (!_isSuccess)
                throw new InvalidOperationException("Cannot access value of a failed result.");
            return _value;
        }
    }

    /// <summary>
    /// Gets the error value. Throws if the result is a success.
    /// </summary>
    public TError Error
    {
        get
        {
            if (_isSuccess)
                throw new InvalidOperationException("Cannot access error of a successful result.");
            return _error;
        }
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T, TError> Success(T value)
    {
        return new Result<T, TError>(value, default(TError)!, true);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T, TError> Failure(TError error)
    {
        return new Result<T, TError>(default(T)!, error, false);
    }

    /// <summary>
    /// Transforms the success value using the provided function.
    /// </summary>
    public Result<TResult, TError> Map<TResult>(Func<T, TResult> mapper)
    {
        return _isSuccess
            ? Result<TResult, TError>.Success(mapper(_value))
            : Result<TResult, TError>.Failure(_error);
    }

    /// <summary>
    /// Transforms the error value using the provided function.
    /// </summary>
    public Result<T, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper)
    {
        return _isSuccess
            ? Result<T, TNewError>.Success(_value)
            : Result<T, TNewError>.Failure(mapper(_error));
    }

    /// <summary>
    /// Applies the provided function if this is a success, otherwise returns the failure.
    /// </summary>
    public Result<TResult, TError> FlatMap<TResult>(Func<T, Result<TResult, TError>> mapper)
    {
        return _isSuccess ? mapper(_value) : Result<TResult, TError>.Failure(_error);
    }

    /// <summary>
    /// Returns the success value if present, otherwise returns the default value.
    /// </summary>
    public T OrElse(T defaultValue)
    {
        return _isSuccess ? _value : defaultValue;
    }

    /// <summary>
    /// Returns the success value if present, otherwise returns the result of the supplier function.
    /// </summary>
    public T OrElse(Func<T> supplier)
    {
        return _isSuccess ? _value : supplier();
    }

    /// <summary>
    /// Returns the success value if present, otherwise returns the result of the supplier function with access to the error.
    /// </summary>
    public T OrElseGet(Func<TError, T> supplier)
    {
        return _isSuccess ? _value : supplier(_error);
    }

    /// <summary>
    /// Returns the success value if present, otherwise returns the result of the supplier function.
    /// </summary>
    public T OrElseGet(Func<T> supplier)
    {
        return _isSuccess ? _value : supplier();
    }

    /// <summary>
    /// Executes the appropriate action based on whether the result is success or failure.
    /// </summary>
    public Result<T, TError> Match(Action<T> onSuccess, Action<TError> onFailure)
    {
        if (_isSuccess)
            onSuccess(_value);
        else
            onFailure(_error);
        return this;
    }

    /// <summary>
    /// Executes and returns the result of the appropriate function based on whether the result is success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        return _isSuccess ? onSuccess(_value) : onFailure(_error);
    }

    /// <summary>
    /// Converts the result to an Optional, returning the value if success, empty if failure.
    /// </summary>
    public Optional<T> ToOptional()
    {
        return _isSuccess ? Optional<T>.Of(_value) : Optional<T>.Empty();
    }

    public bool Equals(Result<T, TError> other)
    {
        if (_isSuccess != other._isSuccess)
            return false;

        if (_isSuccess)
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        else
            return EqualityComparer<TError>.Default.Equals(_error, other._error);
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T, TError> other && Equals(other);
    }

    public override int GetHashCode()
    {
        if (_isSuccess)
            return HashCode.Combine(_isSuccess, _value);
        else
            return HashCode.Combine(_isSuccess, _error);
    }

    public override string ToString()
    {
        return _isSuccess ? $"Success({_value})" : $"Failure({_error})";
    }

    public static bool operator ==(Result<T, TError> left, Result<T, TError> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Result<T, TError> left, Result<T, TError> right)
    {
        return !left.Equals(right);
    }
}