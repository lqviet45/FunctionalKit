namespace FunctionalKit.Core;

/// <summary>
/// Represents a validation result that can accumulate multiple errors.
/// Unlike Result, Validation can collect all validation errors instead of short-circuiting on the first failure.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
/// <typeparam name="TError">The type of the error</typeparam>
public readonly struct Validation<T, TError> : IEquatable<Validation<T, TError>>
{
    private readonly T _value;
    private readonly IReadOnlyList<TError> _errors;
    private readonly bool _isValid;

    private Validation(T value, IReadOnlyList<TError> errors, bool isValid)
    {
        _value = value;
        _errors = errors ?? Array.Empty<TError>();
        _isValid = isValid;
    }

    /// <summary>
    /// Gets a value indicating whether the validation is successful.
    /// </summary>
    public bool IsValid => _isValid;

    /// <summary>
    /// Gets a value indicating whether the validation has errors.
    /// </summary>
    public bool IsInvalid => !_isValid;

    /// <summary>
    /// Gets the success value. Throws if the validation is invalid.
    /// </summary>
    public T Value
    {
        get
        {
            if (!_isValid)
                throw new InvalidOperationException("Cannot access value of an invalid validation.");
            return _value;
        }
    }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<TError> Errors => _errors;

    /// <summary>
    /// Creates a successful validation with the specified value.
    /// </summary>
    public static Validation<T, TError> Success(T value)
    {
        return new Validation<T, TError>(value, Array.Empty<TError>(), true);
    }

    /// <summary>
    /// Creates a failed validation with a single error.
    /// </summary>
    public static Validation<T, TError> Failure(TError error)
    {
        return new Validation<T, TError>(default(T)!, new[] { error }, false);
    }

    /// <summary>
    /// Creates a failed validation with multiple errors.
    /// </summary>
    public static Validation<T, TError> Failure(IEnumerable<TError> errors)
    {
        var errorList = errors.ToList();
        if (!errorList.Any())
            throw new ArgumentException("At least one error must be provided", nameof(errors));

        return new Validation<T, TError>(default(T)!, errorList, false);
    }

    /// <summary>
    /// Creates a failed validation with multiple errors.
    /// </summary>
    public static Validation<T, TError> Failure(params TError[] errors)
    {
        return Failure((IEnumerable<TError>)errors);
    }

    /// <summary>
    /// Transforms the success value using the provided function.
    /// </summary>
    public Validation<TResult, TError> Map<TResult>(Func<T, TResult> mapper)
    {
        return _isValid
            ? Validation<TResult, TError>.Success(mapper(_value))
            : Validation<TResult, TError>.Failure(_errors);
    }

    /// <summary>
    /// Applies the provided function if this is valid, otherwise propagates the errors.
    /// </summary>
    public Validation<TResult, TError> FlatMap<TResult>(Func<T, Validation<TResult, TError>> mapper)
    {
        return _isValid ? mapper(_value) : Validation<TResult, TError>.Failure(_errors);
    }

    /// <summary>
    /// Combines this validation with another, accumulating errors from both if either fails.
    /// </summary>
    public Validation<TResult, TError> Combine<TOther, TResult>(
        Validation<TOther, TError> other,
        Func<T, TOther, TResult> combiner)
    {
        if (_isValid && other.IsValid)
            return Validation<TResult, TError>.Success(combiner(_value, other.Value));

        var allErrors = new List<TError>();
        if (!_isValid) allErrors.AddRange(_errors);
        if (!other.IsValid) allErrors.AddRange(other.Errors);

        return Validation<TResult, TError>.Failure(allErrors);
    }

    /// <summary>
    /// Combines multiple validations into one, accumulating all errors.
    /// </summary>
    public static Validation<IEnumerable<T>, TError> Combine(IEnumerable<Validation<T, TError>> validations)
    {
        var validationList = validations.ToList();
        var allErrors = new List<TError>();
        var values = new List<T>();

        foreach (var validation in validationList)
        {
            if (validation.IsValid)
                values.Add(validation.Value);
            else
                allErrors.AddRange(validation.Errors);
        }

        return allErrors.Any()
            ? Validation<IEnumerable<T>, TError>.Failure(allErrors)
            : Validation<IEnumerable<T>, TError>.Success(values);
    }

    /// <summary>
    /// Returns the success value if valid, otherwise returns the default value.
    /// </summary>
    public T OrElse(T defaultValue)
    {
        return _isValid ? _value : defaultValue;
    }

    /// <summary>
    /// Returns the success value if valid, otherwise returns the result of the supplier function.
    /// </summary>
    public T OrElse(Func<T> supplier)
    {
        return _isValid ? _value : supplier();
    }

    /// <summary>
    /// Returns the success value if valid, otherwise returns the result of the supplier function with access to errors.
    /// </summary>
    public T OrElseGet(Func<IReadOnlyList<TError>, T> supplier)
    {
        return _isValid ? _value : supplier(_errors);
    }

    /// <summary>
    /// Executes the appropriate action based on whether the validation is valid or invalid.
    /// </summary>
    public Validation<T, TError> Match(Action<T> onValid, Action<IReadOnlyList<TError>> onInvalid)
    {
        if (_isValid)
            onValid(_value);
        else
            onInvalid(_errors);
        return this;
    }

    /// <summary>
    /// Executes and returns the result of the appropriate function based on whether the validation is valid or invalid.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onValid, Func<IReadOnlyList<TError>, TResult> onInvalid)
    {
        return _isValid ? onValid(_value) : onInvalid(_errors);
    }

    /// <summary>
    /// Converts the validation to a Result, combining all errors into a single error.
    /// </summary>
    public Result<T, TError> ToResult()
    {
        return _isValid
            ? Result<T, TError>.Success(_value)
            : Result<T, TError>.Failure(_errors.First());
    }

    /// <summary>
    /// Converts the validation to a Result with string error, joining all error messages.
    /// </summary>
    public Result<T> ToResult(Func<TError, string> errorFormatter, string separator = "; ")
    {
        return _isValid
            ? Result<T>.Success(_value)
            : Result<T>.Failure(string.Join(separator, _errors.Select(errorFormatter)));
    }

    /// <summary>
    /// Converts the validation to an Optional, discarding error information.
    /// </summary>
    public Optional<T> ToOptional()
    {
        return _isValid ? Optional<T>.Of(_value) : Optional<T>.Empty();
    }

    /// <summary>
    /// Converts the validation to an Either.
    /// </summary>
    public Either<IReadOnlyList<TError>, T> ToEither()
    {
        return _isValid
            ? Either<IReadOnlyList<TError>, T>.FromRight(_value)
            : Either<IReadOnlyList<TError>, T>.FromLeft(_errors);
    }

    public bool Equals(Validation<T, TError> other)
    {
        if (_isValid != other._isValid)
            return false;

        if (_isValid)
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        else
            return _errors.SequenceEqual(other._errors);
    }

    public override bool Equals(object? obj)
    {
        return obj is Validation<T, TError> other && Equals(other);
    }

    public override int GetHashCode()
    {
        if (_isValid)
            return HashCode.Combine(_isValid, _value);
        else
            return HashCode.Combine(_isValid, _errors.Count);
    }

    public override string ToString()
    {
        return _isValid
            ? $"Valid({_value})"
            : $"Invalid([{string.Join(", ", _errors)}])";
    }

    public static bool operator ==(Validation<T, TError> left, Validation<T, TError> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Validation<T, TError> left, Validation<T, TError> right)
    {
        return !left.Equals(right);
    }

    // Implicit conversion from T to Validation<T, TError>
    public static implicit operator Validation<T, TError>(T value)
    {
        return Success(value);
    }
}

/// <summary>
/// Simplified Validation type using string for errors.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public readonly struct Validation<T> : IEquatable<Validation<T>>
{
    private readonly Validation<T, string> _inner;

    private Validation(Validation<T, string> inner)
    {
        _inner = inner;
    }

    public bool IsValid => _inner.IsValid;
    public bool IsInvalid => _inner.IsInvalid;
    public T Value => _inner.Value;
    public IReadOnlyList<string> Errors => _inner.Errors;

    public static Validation<T> Success(T value) => new(Validation<T, string>.Success(value));
    public static Validation<T> Failure(string error) => new(Validation<T, string>.Failure(error));
    public static Validation<T> Failure(IEnumerable<string> errors) => new(Validation<T, string>.Failure(errors));
    public static Validation<T> Failure(params string[] errors) => new(Validation<T, string>.Failure(errors));

    public Validation<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        new(_inner.Map(mapper));

    public Validation<TResult> FlatMap<TResult>(Func<T, Validation<TResult>> mapper) =>
        IsValid ? mapper(_inner.Value) : Validation<TResult>.Failure(_inner.Errors);

    public Validation<TResult> Combine<TOther, TResult>(
        Validation<TOther> other,
        Func<T, TOther, TResult> combiner) =>
        new(_inner.Combine(other._inner, combiner));

    public T OrElse(T defaultValue) => _inner.OrElse(defaultValue);
    public T OrElse(Func<T> supplier) => _inner.OrElse(supplier);
    public T OrElseGet(Func<IReadOnlyList<string>, T> supplier) => _inner.OrElseGet(supplier);

    public Validation<T> Match(Action<T> onValid, Action<IReadOnlyList<string>> onInvalid)
    {
        _inner.Match(onValid, onInvalid);
        return this;
    }

    public TResult Match<TResult>(Func<T, TResult> onValid, Func<IReadOnlyList<string>, TResult> onInvalid) =>
        _inner.Match(onValid, onInvalid);

    public Result<T> ToResult(string separator = "; ") => _inner.ToResult(x => x, separator);
    public Optional<T> ToOptional() => _inner.ToOptional();

    public bool Equals(Validation<T> other) => _inner.Equals(other._inner);
    public override bool Equals(object? obj) => obj is Validation<T> other && Equals(other);
    public override int GetHashCode() => _inner.GetHashCode();
    public override string ToString() => _inner.ToString();

    public static bool operator ==(Validation<T> left, Validation<T> right) => left.Equals(right);
    public static bool operator !=(Validation<T> left, Validation<T> right) => !left.Equals(right);

    public static implicit operator Validation<T>(T value) => Success(value);
}