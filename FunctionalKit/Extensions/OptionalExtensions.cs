namespace FunctionalKit.Extensions;

using Core;

public static class OptionalExtensions
{
    /// <summary>
    /// Converts an IEnumerable to Optional of first element.
    /// </summary>
    public static Optional<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        return source.FirstOrDefault() is T item ? Optional<T>.Of(item) : Optional<T>.Empty();
    }

    /// <summary>
    /// Converts an IEnumerable to Optional of first element matching predicate.
    /// </summary>
    public static Optional<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).FirstOrNone();
    }

    /// <summary>
    /// Flattens Optional<Optional<T>> to Optional<T>.
    /// </summary>
    public static Optional<T> Flatten<T>(this Optional<Optional<T>> optional)
    {
        return optional.FlatMap(x => x);
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
}