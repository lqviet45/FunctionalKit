namespace FunctionalKit.Core;

/// <summary>
/// Represents a value that can be one of two types: Left or Right.
/// By convention, Left represents an error/failure and Right represents success.
/// </summary>
/// <typeparam name="TLeft">The type of the left value (typically error)</typeparam>
/// <typeparam name="TRight">The type of the right value (typically success)</typeparam>
public readonly struct Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>
{
    private readonly TLeft _left;
    private readonly TRight _right;
    private readonly bool _isRight;

    private Either(TLeft left, TRight right, bool isRight)
    {
        _left = left;
        _right = right;
        _isRight = isRight;
    }

    /// <summary>
    /// Gets a value indicating whether this Either contains a Right value.
    /// </summary>
    public bool IsRight => _isRight;

    /// <summary>
    /// Gets a value indicating whether this Either contains a Left value.
    /// </summary>
    public bool IsLeft => !_isRight;

    /// <summary>
    /// Gets the Left value. Throws if this is a Right.
    /// </summary>
    public TLeft Left
    {
        get
        {
            if (_isRight)
                throw new InvalidOperationException("Cannot access Left value of a Right Either.");
            return _left;
        }
    }

    /// <summary>
    /// Gets the Right value. Throws if this is a Left.
    /// </summary>
    public TRight Right
    {
        get
        {
            if (!_isRight)
                throw new InvalidOperationException("Cannot access Right value of a Left Either.");
            return _right;
        }
    }

    /// <summary>
    /// Creates a Left Either with the specified value.
    /// </summary>
    public static Either<TLeft, TRight> FromLeft(TLeft value)
    {
        return new Either<TLeft, TRight>(value, default(TRight)!, false);
    }

    /// <summary>
    /// Creates a Right Either with the specified value.
    /// </summary>
    public static Either<TLeft, TRight> FromRight(TRight value)
    {
        return new Either<TLeft, TRight>(default(TLeft)!, value, true);
    }

    /// <summary>
    /// Maps the Right value using the provided function, leaving Left unchanged.
    /// </summary>
    public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> mapper)
    {
        return _isRight
            ? Either<TLeft, TResult>.FromRight(mapper(_right))
            : Either<TLeft, TResult>.FromLeft(_left);
    }

    /// <summary>
    /// Maps the Left value using the provided function, leaving Right unchanged.
    /// </summary>
    public Either<TResult, TRight> MapLeft<TResult>(Func<TLeft, TResult> mapper)
    {
        return _isRight
            ? Either<TResult, TRight>.FromRight(_right)
            : Either<TResult, TRight>.FromLeft(mapper(_left));
    }

    /// <summary>
    /// Applies the provided function if this is a Right, otherwise returns the Left.
    /// </summary>
    public Either<TLeft, TResult> FlatMap<TResult>(Func<TRight, Either<TLeft, TResult>> mapper)
    {
        return _isRight ? mapper(_right) : Either<TLeft, TResult>.FromLeft(_left);
    }

    /// <summary>
    /// Applies the provided function if this is a Left, otherwise returns the Right.
    /// </summary>
    public Either<TResult, TRight> FlatMapLeft<TResult>(Func<TLeft, Either<TResult, TRight>> mapper)
    {
        return _isRight ? Either<TResult, TRight>.FromRight(_right) : mapper(_left);
    }

    /// <summary>
    /// Returns the Right value if present, otherwise returns the default value.
    /// </summary>
    public TRight OrElse(TRight defaultValue)
    {
        return _isRight ? _right : defaultValue;
    }

    /// <summary>
    /// Returns the Right value if present, otherwise returns the result of the supplier function.
    /// </summary>
    public TRight OrElse(Func<TRight> supplier)
    {
        return _isRight ? _right : supplier();
    }

    /// <summary>
    /// Returns the Right value if present, otherwise returns the result of the supplier function with access to the Left value.
    /// </summary>
    public TRight OrElseGet(Func<TLeft, TRight> supplier)
    {
        return _isRight ? _right : supplier(_left);
    }

    /// <summary>
    /// Executes the appropriate action based on whether this is Left or Right.
    /// </summary>
    public Either<TLeft, TRight> Match(Action<TLeft> onLeft, Action<TRight> onRight)
    {
        if (_isRight)
            onRight(_right);
        else
            onLeft(_left);
        return this;
    }

    /// <summary>
    /// Executes and returns the result of the appropriate function based on whether this is Left or Right.
    /// </summary>
    public TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
    {
        return _isRight ? onRight(_right) : onLeft(_left);
    }

    /// <summary>
    /// Swaps Left and Right sides of the Either.
    /// </summary>
    public Either<TRight, TLeft> Swap()
    {
        return _isRight
            ? Either<TRight, TLeft>.FromLeft(_right)
            : Either<TRight, TLeft>.FromRight(_left);
    }

    /// <summary>
    /// Converts Either to Result, treating Left as error and Right as success.
    /// </summary>
    public Result<TRight, TLeft> ToResult()
    {
        return _isRight
            ? Result<TRight, TLeft>.Success(_right)
            : Result<TRight, TLeft>.Failure(_left);
    }

    /// <summary>
    /// Converts Either to Optional, returning Right value or empty.
    /// </summary>
    public Optional<TRight> ToOptional()
    {
        return _isRight ? Optional<TRight>.Of(_right) : Optional<TRight>.Empty();
    }

    /// <summary>
    /// Filters the Right value with a predicate, converting to Left if the predicate fails.
    /// </summary>
    public Either<TLeft, TRight> Filter(Func<TRight, bool> predicate, TLeft leftValue)
    {
        if (!_isRight)
            return this;

        return predicate(_right) ? this : FromLeft(leftValue);
    }

    /// <summary>
    /// Filters the Right value with a predicate, converting to Left using a supplier if the predicate fails.
    /// </summary>
    public Either<TLeft, TRight> Filter(Func<TRight, bool> predicate, Func<TRight, TLeft> leftSupplier)
    {
        if (!_isRight)
            return this;

        return predicate(_right) ? this : FromLeft(leftSupplier(_right));
    }

    public bool Equals(Either<TLeft, TRight> other)
    {
        if (_isRight != other._isRight)
            return false;

        if (_isRight)
            return EqualityComparer<TRight>.Default.Equals(_right, other._right);
        else
            return EqualityComparer<TLeft>.Default.Equals(_left, other._left);
    }

    public override bool Equals(object? obj)
    {
        return obj is Either<TLeft, TRight> other && Equals(other);
    }

    public override int GetHashCode()
    {
        if (_isRight)
            return HashCode.Combine(_isRight, _right);
        else
            return HashCode.Combine(_isRight, _left);
    }

    public override string ToString()
    {
        return _isRight ? $"Right({_right})" : $"Left({_left})";
    }

    public static bool operator ==(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return !left.Equals(right);
    }

    // Implicit conversions
    public static implicit operator Either<TLeft, TRight>(TLeft left)
    {
        return FromLeft(left);
    }

    public static implicit operator Either<TLeft, TRight>(TRight right)
    {
        return FromRight(right);
    }
}