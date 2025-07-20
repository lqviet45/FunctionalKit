namespace FunctionalKit.Core;

/// <summary>
/// Represents an optional value that may or may not be present, similar to Java's Optional.
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    private readonly T _value;
    private readonly bool _hasValue;

    private Optional(T value, bool hasValue)
    {
        _value = value;
        _hasValue = hasValue;
    }

    /// <summary>
    /// Gets a value indicating whether the optional contains a value.
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// Gets a value indicating whether the optional is empty.
    /// </summary>
    public bool IsEmpty => !_hasValue;

    /// <summary>
    /// Gets the value if present, otherwise throws InvalidOperationException.
    /// </summary>
    public T Value
    {
        get
        {
            if (!_hasValue)
                throw new InvalidOperationException("Optional does not contain a value.");
            return _value;
        }
    }

    /// <summary>
    /// Creates an Optional with the specified value.
    /// </summary>
    public static Optional<T> Of(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Optional<T>(value, true);
    }

    /// <summary>
    /// Creates an Optional that may contain null.
    /// </summary>
    public static Optional<T> OfNullable(T? value)
    {
        return value == null ? Empty() : new Optional<T>(value, true);
    }

    /// <summary>
    /// Creates an empty Optional.
    /// </summary>
    public static Optional<T> Empty()
    {
        return new Optional<T>(default(T)!, false);
    }

    /// <summary>
    /// Returns the value if present, otherwise returns the specified default value.
    /// </summary>
    public T OrElse(T defaultValue)
    {
        return _hasValue ? _value : defaultValue;
    }

    /// <summary>
    /// Returns the value if present, otherwise returns the result of the supplier function.
    /// </summary>
    public T OrElseGet(Func<T> supplier)
    {
        return _hasValue ? _value : supplier();
    }

    /// <summary>
    /// Returns the value if present, otherwise throws the exception provided by the supplier.
    /// </summary>
    public T OrElseThrow<TException>(Func<TException> exceptionSupplier) where TException : Exception
    {
        if (_hasValue)
            return _value;
        throw exceptionSupplier();
    }

    /// <summary>
    /// If a value is present, applies the provided function and returns an Optional describing the result.
    /// </summary>
    public Optional<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        if (!_hasValue)
            return Optional<TResult>.Empty();

        var result = mapper(_value);
        return Optional<TResult>.OfNullable(result);
    }

    /// <summary>
    /// If a value is present, applies the provided Optional-bearing function and returns the result.
    /// </summary>
    public Optional<TResult> FlatMap<TResult>(Func<T, Optional<TResult>> mapper)
    {
        return _hasValue ? mapper(_value) : Optional<TResult>.Empty();
    }

    /// <summary>
    /// If a value is present and matches the predicate, returns this Optional, otherwise returns empty.
    /// </summary>
    public Optional<T> Filter(Func<T, bool> predicate)
    {
        if (!_hasValue)
            return this;

        return predicate(_value) ? this : Empty();
    }

    /// <summary>
    /// If a value is present, performs the given action with the value.
    /// </summary>
    public Optional<T> IfPresent(Action<T> action)
    {
        if (_hasValue)
            action(_value);
        return this;
    }

    /// <summary>
    /// If a value is present, performs the given action, otherwise performs the empty action.
    /// </summary>
    public Optional<T> IfPresentOrElse(Action<T> action, Action emptyAction)
    {
        if (_hasValue)
            action(_value);
        else
            emptyAction();
        return this;
    }

    public bool Equals(Optional<T> other)
    {
        if (_hasValue != other._hasValue)
            return false;

        if (!_hasValue)
            return true;

        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is Optional<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hasValue ? EqualityComparer<T>.Default.GetHashCode(_value!) : 0;
    }

    public override string ToString()
    {
        return _hasValue ? $"Optional[{_value}]" : "Optional.Empty";
    }

    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !left.Equals(right);
    }

    // Implicit conversion from T to Optional<T>
    public static implicit operator Optional<T>(T value)
    {
        return OfNullable(value);
    }
}