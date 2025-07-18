namespace FunctionalKit.Extensions;

using Core;

public static class OptionalExtensions
{
    /// <summary>
    /// Converts an IEnumerable to Optional of first element.
    /// </summary>
    public static Optional<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        return source.FirstOrDefault() is { } item ? Optional<T>.Of(item) : Optional<T>.Empty();
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of first element matching predicate.
    /// </summary>
    public static Optional<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).FirstOrNone();
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of last element.
    /// </summary>
    public static Optional<T> LastOrNone<T>(this IEnumerable<T> source)
    {
        return source.LastOrDefault() is { } item ? Optional<T>.Of(item) : Optional<T>.Empty();
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of last element matching predicate.
    /// </summary>
    public static Optional<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).LastOrNone();
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of single element.
    /// </summary>
    public static Optional<T> SingleOrNone<T>(this IEnumerable<T> source)
    {
        try
        {
            var result = source.SingleOrDefault();
            return result is not null ? Optional<T>.Of(result) : Optional<T>.Empty();
        }
        catch (InvalidOperationException)
        {
            return Optional<T>.Empty();
        }
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of single element matching predicate.
    /// </summary>
    public static Optional<T> SingleOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).SingleOrNone();
    }

    /// <summary>
    /// Finds an element in a collection and returns it as Optional.
    /// </summary>
    public static Optional<T> Find<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).FirstOrNone();
    }

    /// <summary>
    /// Safely gets a value from a dictionary.
    /// </summary>
    public static Optional<TValue> GetOptional<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.TryGetValue(key, out var value) ? Optional<TValue>.OfNullable(value) : Optional<TValue>.Empty();
    }

    /// <summary>
    /// Safely gets an element at the specified index.
    /// </summary>
    public static Optional<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
    {
        try
        {
            var result = source.ElementAtOrDefault(index);
            return result is not null ? Optional<T>.Of(result) : Optional<T>.Empty();
        }
        catch (ArgumentOutOfRangeException)
        {
            return Optional<T>.Empty();
        }
    }

    /// <summary>
    /// Flattens Optional<Optional<T>> to Optional<T>.
    /// </summary>
    public static Optional<T> Flatten<T>(this Optional<Optional<T>> optional)
    {
        return optional.FlatMap(x => x);
    }

    /// <summary>
    /// Filters a collection of Optionals to only present values.
    /// </summary>
    public static IEnumerable<T> CatOptionals<T>(this IEnumerable<Optional<T>> source)
    {
        return source.Where(opt => opt.HasValue).Select(opt => opt.Value);
    }

    /// <summary>
    /// Maps over a collection and filters out empty results.
    /// </summary>
    public static IEnumerable<TResult> MapOptional<T, TResult>(this IEnumerable<T> source, Func<T, Optional<TResult>> mapper)
    {
        return source.Select(mapper).CatOptionals();
    }

    /// <summary>
    /// Converts Optional to nullable reference.
    /// </summary>
    public static T? ToNullable<T>(this Optional<T> optional) where T : class
    {
        return optional.HasValue ? optional.Value : null;
    }

    /// <summary>
    /// Converts Optional to nullable value type.
    /// </summary>
    public static T? ToNullableValue<T>(this Optional<T> optional) where T : struct
    {
        return optional.HasValue ? optional.Value : null;
    }

    /// <summary>
    /// Converts Optional to Result with custom error message.
    /// </summary>
    public static Result<T> ToResult<T>(this Optional<T> optional, string errorMessage)
    {
        return optional.HasValue 
            ? Result<T>.Success(optional.Value) 
            : Result<T>.Failure(errorMessage);
    }

    /// <summary>
    /// Converts Optional to Result using error factory.
    /// </summary>
    public static Result<T> ToResult<T>(this Optional<T> optional, Func<string> errorFactory)
    {
        return optional.HasValue 
            ? Result<T>.Success(optional.Value) 
            : Result<T>.Failure(errorFactory());
    }

    /// <summary>
    /// Converts Optional to Either.
    /// </summary>
    public static Either<TLeft, T> ToEither<TLeft, T>(this Optional<T> optional, TLeft leftValue)
    {
        return optional.HasValue 
            ? Either<TLeft, T>.FromRight(optional.Value)
            : Either<TLeft, T>.FromLeft(leftValue);
    }

    /// <summary>
    /// Converts Optional to Either using factory.
    /// </summary>
    public static Either<TLeft, T> ToEither<TLeft, T>(this Optional<T> optional, Func<TLeft> leftFactory)
    {
        return optional.HasValue 
            ? Either<TLeft, T>.FromRight(optional.Value)
            : Either<TLeft, T>.FromLeft(leftFactory());
    }

    /// <summary>
    /// Zips two Optionals together.
    /// </summary>
    public static Optional<TResult> Zip<T1, T2, TResult>(
        this Optional<T1> optional1, 
        Optional<T2> optional2, 
        Func<T1, T2, TResult> zipper)
    {
        if (optional1.HasValue && optional2.HasValue)
            return Optional<TResult>.Of(zipper(optional1.Value, optional2.Value));
        
        return Optional<TResult>.Empty();
    }

    /// <summary>
    /// Combines multiple Optionals - returns Some if all have values, None otherwise.
    /// </summary>
    public static Optional<IEnumerable<T>> Sequence<T>(this IEnumerable<Optional<T>> optionals)
    {
        var optionalList = optionals.ToList();
        
        if (optionalList.All(opt => opt.HasValue))
        {
            var values = optionalList.Select(opt => opt.Value);
            return Optional<IEnumerable<T>>.Of(values);
        }
        
        return Optional<IEnumerable<T>>.Empty();
    }

    /// <summary>
    /// Alternative to OrElse that works with Optional.
    /// </summary>
    public static Optional<T> Or<T>(this Optional<T> optional, Optional<T> alternative)
    {
        return optional.HasValue ? optional : alternative;
    }

    /// <summary>
    /// Alternative to OrElse that works with Optional factory.
    /// </summary>
    public static Optional<T> Or<T>(this Optional<T> optional, Func<Optional<T>> alternativeFactory)
    {
        return optional.HasValue ? optional : alternativeFactory();
    }
}